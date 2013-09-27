// Octopus MFS is an integrated suite for managing a Micro Finance Institution: 
// clients, contracts, accounting, reporting and risk
// Copyright © 2006,2007 OCTO Technology & OXUS Development Network
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//
// Website: http://www.opencbs.com
// Contact: contact@opencbs.com

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts;
using OpenCBS.CoreDomain.Contracts.Collaterals;
using OpenCBS.CoreDomain.Contracts.Guarantees;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.EconomicActivities;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.CoreDomain.Events.Saving;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.Online;
using OpenCBS.CoreDomain.Products;
using OpenCBS.CoreDomain.Products.Collaterals;
using OpenCBS.Enums;
using OpenCBS.ExceptionsHandler;
using OpenCBS.ExceptionsHandler.Exceptions.SavingExceptions;
using OpenCBS.Extensions;
using OpenCBS.GUI.Contracts;
using OpenCBS.GUI.Tools;
using OpenCBS.GUI.UserControl;
using OpenCBS.MultiLanguageRessources;
using OpenCBS.Reports;
using OpenCBS.Services;
using OpenCBS.Shared;
using OpenCBS.Shared.Settings;
using Group = OpenCBS.CoreDomain.Clients.Group;

namespace OpenCBS.GUI.Clients
{
    using ML = MultiLanguageStrings;
    public partial class ClientForm : SweetForm
    {
        #region *** Fields ***
        private LoanProduct _product;
        private Project _project;
        private Group _group;
        private Loan _credit;
        private Loan _copyOfCredit;
        private Guarantee _guarantee;
        private Person _person;
        private readonly Form _mdiParent;

        private OClientTypes _oClientType;
        private PersonUserControl _personUserControl;
        private GroupUserControl _groupUserControl;
        private readonly bool _closeFormAfterSave;
        private List<LoanShare> _loanShares;
        private List<User> _users;
        private FundingLine _fundingLine;
        private Corporate _corporate;
        private CorporateUserControl _corporateUserControl;
        private List<FollowUp> _followUpList = new List<FollowUp>();
        private SavingsBookProduct _savingsBookProduct;
        private SavingBookContract _saving;
        private bool _toChangeAlignDate;
        private int? _gracePeriodOfLateFees;
        private string _title;
        private Client _client;
        private DateTime _firstInstallmentDate;

        OCurrency _totalGuarantorAmount = 0;
        OCurrency _totalCollateralAmount = 0;

        private List<Guarantor> _listGuarantors;
        private List<ContractCollateral> _collaterals;
        private string _typeOfFee;
        private DoubleValueRange _anticipatedTotalFeesValueRange;
        private DoubleValueRange _anticipatedPartialFeesValueRange;

        private CustomizableFieldsControl _customizableLoanFieldsControl;
        private CustomizableFieldsControl _customizableSavingsFieldsControl;


        private DateTime _oldDisbursmentDate;
        private DateTime _oldFirstInstalmentDate;
        private bool _changeDisDateBool;

        [ImportMany(typeof(ILoanTabs), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public List<ILoanTabs> LoanExtensions { get; set; }

        [ImportMany(typeof(ISavingsTabs), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public List<ISavingsTabs> SavingsExtensions { get; set; }

        [ImportMany(typeof(ILoanDetailsButton), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public List<ILoanDetailsButton> LoanDetailsButtons { get; set; }

        #endregion

        #region *** Constructors ***

        private ClientForm()
        {
            MefContainer.Current.Bind(this);
        }

        public ClientForm(OClientTypes pClientType, Form pMdiParent, bool pCloseFormAfterSave)
            : this()
        {
            _listGuarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _loanShares = new List<LoanShare>();
            _closeFormAfterSave = pCloseFormAfterSave;
            _mdiParent = pMdiParent;
            InitializeComponent();
            InitControls();
            _oClientType = pClientType;

            if (pClientType == OClientTypes.Person) _person = new Person();
            else if (pClientType == OClientTypes.Group) _group = new Group();
            else _corporate = new Corporate();
            InitializeUserControl(pClientType, pMdiParent);
            InitializeTitle(null);
        }

        private void InitControls()
        {
            var contractStatusItems = cmbContractStatus.Items;
            var firstItem = GetContractStatusItem(OContractStatus.Pending);
            contractStatusItems.Add(firstItem);
            contractStatusItems.Add(GetContractStatusItem(OContractStatus.Postponed));
            contractStatusItems.Add(GetContractStatusItem(OContractStatus.Validated));
            contractStatusItems.Add(GetContractStatusItem(OContractStatus.Refused));
            contractStatusItems.Add(GetContractStatusItem(OContractStatus.Abandoned));
            cmbContractStatus.SelectedItem = firstItem;

            ApplicationSettings dataParam = ApplicationSettings.GetInstance(string.Empty);
            int decimalPlaces = dataParam.InterestRateDecimalPlaces;
            nudInterestRate.DecimalPlaces = decimalPlaces;

            decimal increment = decimal.One;
            for (int exp = 0; exp < decimalPlaces; exp++)
                increment = decimal.Divide(increment, 10m);
            nudInterestRate.Increment = increment;
        }

        private KeyValuePair<OContractStatus, string> GetContractStatusItem(OContractStatus status)
        {
            string statusName = status.GetName();
            string statusText = GetString(string.Format("{0}.Text", statusName));
            return new KeyValuePair<OContractStatus, string>(status, statusText);
        }

        public ClientForm(Person pPerson, Form pMdiParent)
            : this()
        {
            _mdiParent = pMdiParent;
            _person = pPerson;
            _client = pPerson;
            InitLoanOfficers(_client);
            _listGuarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _loanShares = new List<LoanShare>();

            InitializeComponent();
            InitControls();
            _oClientType = OClientTypes.Person;
            InitializeUserControl(OClientTypes.Person, pMdiParent);
            InitializeTitle(string.Format("{0} {1}", pPerson.FirstName, pPerson.LastName));
        }

        public ClientForm(Corporate pCorporate, Form pMdiParent)
            : this()
        {
            _mdiParent = pMdiParent;
            _corporate = pCorporate;
            _client = pCorporate;
            InitLoanOfficers(_client);
            _listGuarantors = new List<Guarantor>();

            _collaterals = new List<ContractCollateral>();
            _loanShares = new List<LoanShare>();
            InitializeComponent();
            InitControls();
            _oClientType = OClientTypes.Corporate;
            InitializeUserControl(OClientTypes.Corporate, pMdiParent);
            InitializeTitle(_corporate.Name);
        }

        public ClientForm(Group pGroup, Form pMdiParent)
            : this()
        {
            _mdiParent = pMdiParent;
            _group = pGroup;
            _client = pGroup;
            InitLoanOfficers(_client);
            _listGuarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _loanShares = new List<LoanShare>();

            InitializeComponent();
            InitControls();
            _oClientType = OClientTypes.Group;
            InitializeUserControl(OClientTypes.Group, pMdiParent);
            InitializeTitle(_group.Name);
        }

        public ClientForm(IClient pClient, int pContractId, Form pMdiParent)
            : this()
        {
            _mdiParent = pMdiParent;
            _listGuarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _loanShares = new List<LoanShare>();

            _client = (Client)pClient;
            InitLoanOfficers(_client);

            InitializeComponent();
            InitControls();
            InitializeClient(pClient, pContractId);
            if (_credit != null)
                foreach (LoanShare ls in _credit.LoanShares)
                    _loanShares.Add(ls);

            InitializeTabPageAdvancedSettings();

            bool active = _credit != null && _credit.ContractStatus == OContractStatus.Active;
            InitializeCustomizableFields(OCustomizableFieldEntities.Loan, pContractId, active);
            LoadLoanDetailsExtensions();
            LoadLoanDetailsButtons();
        }

        public ClientForm(IClient pClient, int pContractId, Form pMdiParent, string selectedTab)
            : this()
        {
            _mdiParent = pMdiParent;
            _listGuarantors = new List<Guarantor>();
            _collaterals = new List<ContractCollateral>();
            _loanShares = new List<LoanShare>();

            _client = (Client)pClient;
            InitLoanOfficers(_client);

            InitializeComponent();
            InitControls();
            InitializeClient(pClient, pContractId);
            if (_credit != null)
                foreach (LoanShare ls in _credit.LoanShares)
                    _loanShares.Add(ls);

            tabControlPerson.SelectTab(selectedTab);
            InitializeTabPageAdvancedSettings();

            bool active = _credit != null && _credit.ContractStatus == OContractStatus.Active;
            InitializeCustomizableFields(OCustomizableFieldEntities.Loan, pContractId, active);
            LoadLoanDetailsExtensions();
            LoadLoanDetailsButtons();
        }
        #endregion

        public Project Project
        {
            set { _project = value; }
        }

        // Pasha BASTOV:
        // TODO: this is a temporary solution.
        // This operation should be carried out in fact
        // in the service layer (ClientServices).
        // If you dare to refactor this remember it might
        // take considreable amoutn of time. That's the reason
        // I have written this temp method.

        private void InitLoanOfficers(IClient client)
        {
            UserServices us = ServiceProvider.GetUserServices();
            foreach (Project p in client.Projects)
            {
                foreach (Loan l in p.Credits)
                {
                    l.LoanOfficer = us.Find(l.LoanOfficer.Id);
                }
            }
        }

        private static IServices ServiceProvider
        {
            get { return ServicesProvider.GetInstance(); }
        }

        private static SavingServices SavingServices
        {
            get { return ServiceProvider.GetSavingServices(); }
        }

        public Person Person
        {
            get { return _person; }
        }

        private void InitializeContractStatus(Loan pCredit)
        {
            if (pCredit.WrittenOff)
            {
                lblLoanStatus.Text = MultiLanguageStrings.GetString(Ressource.CreditContractForm, "statusWriteOff.Text");
                lblLoanStatus.ForeColor = Color.FromArgb(0, 0, 0);
                lblLoanStatus.Visible = true;
            }
            else if (pCredit.Closed)
            {
                lblLoanStatus.Text = MultiLanguageStrings.GetString(Ressource.CreditContractForm, "fullyRepaid.Text");
                lblLoanStatus.ForeColor = Color.FromArgb(61, 153, 57);
                lblLoanStatus.Visible = true;
            }
            else if (pCredit.Rescheduled)
            {
                lblLoanStatus.Text = MultiLanguageStrings.GetString(Ressource.CreditContractForm, "statusRescheduled.Text");
                lblLoanStatus.ForeColor = Color.RoyalBlue;
                lblLoanStatus.Visible = true;
            }
            else if (pCredit.BadLoan)
            {
                lblLoanStatus.Text = MultiLanguageStrings.GetString(Ressource.CreditContractForm, "statusBadLoan.Text");
                lblLoanStatus.ForeColor = Color.FromArgb(255, 92, 92);
                lblLoanStatus.Visible = true;
            }
            else
            {
                /*lblLoanStatus.Text = MultiLanguageStrings.GetString(Ressource.CreditContractForm, "contract.Text");
                lblLoanStatus.ForeColor = Color.White;*/
            }
        }

        public void DisplayClient(IClient pClient, Project project)
        {
            if (pClient is Person)
            {
                _person = (Person)pClient;
                _oClientType = OClientTypes.Person;
                InitializeUserControl(_oClientType, _mdiParent);
                InitializeTitle(string.Format("{0} {1}", _person.FirstName, _person.LastName));
            }
            if (pClient is Group)
            {
                _group = (Group)pClient;
                _oClientType = OClientTypes.Group;
                InitializeUserControl(_oClientType, _mdiParent);
                InitializeTitle(_group.Name);
            }
            if (pClient is Corporate)
            {
                _corporate = (Corporate)pClient;
                _oClientType = OClientTypes.Corporate;
                InitializeUserControl(_oClientType, _mdiParent);
                InitializeTitle(_corporate.Name);
            }

            tabControlPerson.TabPages.Add(tabPageProject);
            DisplaySelectedProject(_project);
        }

        private void InitializeClient(IClient pClient, int pContractId)
        {
            if (pClient is Person)
            {
                _person = (Person)pClient;
                _client = _person;
                _oClientType = OClientTypes.Person;
                InitializeUserControl(_oClientType, _mdiParent);
                InitializeTitle(string.Format("{0} {1}", _person.FirstName, _person.LastName));
            }
            if (pClient is Group)
            {
                _group = (Group)pClient;
                _client = _group;
                _oClientType = OClientTypes.Group;
                InitializeUserControl(_oClientType, _mdiParent);
                InitializeTitle(_group.Name);
            }
            if (pClient is Corporate)
            {
                _corporate = (Corporate)pClient;
                _client = _corporate;
                _oClientType = OClientTypes.Corporate;
                InitializeUserControl(_oClientType, _mdiParent);
                InitializeTitle(_corporate.Name);
            }

            _project = pClient.SelectProject(pContractId);
            _credit = _project.SelectCredit(pContractId);
            _credit.LoanEntryFeesList = ServicesProvider.GetInstance().GetContractServices().GetInstalledLoanEntryFees(_credit);
            _product = _credit.Product;

            if (_product.CycleId != null && _credit.Disbursed == false)
            {
                ServicesProvider.GetInstance().GetProductServices().SetCyclesParamsForContract(_product, _credit, _client, false);
            }
            else
            {
                if (_credit.Product.Amount.HasValue == false)
                {
                    _amountValueRange = new DecimalValueRange(_credit.Product.AmountMin.Value, _credit.Product.AmountMax.Value);
                    nudLoanAmount.Minimum = _credit.Product.AmountMin.Value;
                    nudLoanAmount.Maximum = _credit.Product.AmountMax.Value;
                }
                else
                {
                    nudLoanAmount.Minimum = nudLoanAmount.Maximum = _credit.Product.Amount.Value;
                }
                if (_credit.Product.NbOfInstallments.HasValue == false)
                {
                    nudLoanNbOfInstallments.Minimum = _credit.Product.NbOfInstallmentsMin.Value;
                    nudLoanNbOfInstallments.Maximum = _credit.Product.NbOfInstallmentsMax.Value;
                }
                else
                {
                    nudLoanNbOfInstallments.Minimum = nudLoanNbOfInstallments.Maximum = _credit.Product.NbOfInstallments.Value;
                }
            }

            if (ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
            {
                tabControlPerson.TabPages.Add(tabPageProject);
                DisplaySelectedProject(_project);
            }
            else
            {
                AddProject(_project);
            }

            tabControlPerson.TabPages.Add(tabPageLoansDetails);
            tabControlPerson.TabPages.Add(tabPageAdvancedSettings);
            tabControlPerson.TabPages.Add(tabPageLoanGuarantees);

            _credit.LoanInitialOfficer = _credit.LoanOfficer;

            InitializeTabPageLoansDetails(_credit);

            tabControlPerson.TabPages.Add(tabPageCreditCommitee);
            if (_credit.Disbursed)
            {
                tabControlPerson.TabPages.Add(tabPageLoanRepayment);
                //tabControlPerson.TabPages.Add(tabPageLAC);
                tabControlPerson.SelectedTab = tabPageLoanRepayment;
                InitializeTabPageLoanRepayment(_credit);
            }
            else if (_credit.ContractStatus == OContractStatus.Validated && !_credit.Disbursed)
            {
                tabControlPerson.SelectedTab = tabPageLoansDetails;
            }
        }

        private void InitializeProjectAddress()
        {
            projectAddressUserControl = new AddressUserControl();
            projectAddressUserControl.Dock = DockStyle.Fill;
            gBProjectAddress.Controls.Add(projectAddressUserControl);
        }

        private void InitializeTitle(string title)
        {
            if (_person != null)
            {
                Text = string.IsNullOrEmpty(title) ? MultiLanguageStrings.GetString(Ressource.ClientForm, "Person.Text") : title;
                if (_person.BadClient)
                {
                    Text += "  " + MultiLanguageStrings.GetString(Ressource.ClientForm, "Bad.Text");
                    lblTitle.BackColor = Color.Red;
                }
                else
                {
                    lblTitle.BackColor = Color.FromArgb(0, 81, 152);
                }
            }
            else if (_group != null)
            {
                Text = string.IsNullOrEmpty(title) ? MultiLanguageStrings.GetString(Ressource.ClientForm, "Group.Text") : title + " - (" + _group.LoanCycle + ")";

                if (_group.BadClient)
                {
                    Text += "  " + MultiLanguageStrings.GetString(Ressource.ClientForm, "Bad.Text");
                    lblTitle.BackColor = Color.Red;
                }
                else
                {
                    lblTitle.BackColor = Color.FromArgb(0, 81, 152);
                }
            }
            else
            {
                Text = string.IsNullOrEmpty(title) ? MultiLanguageStrings.GetString(Ressource.ClientForm, "Corporate.Text") : title;
                if (_corporate.BadClient)
                {
                    Text += "  " + MultiLanguageStrings.GetString(Ressource.ClientForm, "Bad.Text");
                    lblTitle.BackColor = Color.Red;
                }
                else
                {
                    lblTitle.BackColor = Color.FromArgb(0, 81, 152);
                }
            }
            _title = Text;
        }

        private void InitializeUserControl(OClientTypes pClientType, Form pMdiParent)
        {
            btnLoanShares.Visible = OClientTypes.Group == pClientType;

            tabControlPerson.TabPages.Remove(tabPageProject);
            tabControlPerson.TabPages.Remove(tabPageCreditCommitee);
            tabControlPerson.TabPages.Remove(tabPageLoansDetails);
            tabControlPerson.TabPages.Remove(tabPageAdvancedSettings);
            tabControlPerson.TabPages.Remove(tabPageLoanRepayment);
            tabControlPerson.TabPages.Remove(tabPageLoanGuarantees);
            tabControlPerson.TabPages.Remove(tabPageSavingDetails);
            tabControlPerson.TabPages.Remove(tabPageContracts);

            InitializeProjectAddress();
            
            if (pClientType == OClientTypes.Person)
            {
                _personUserControl = new PersonUserControl(_person, pMdiParent)
                {
                    Dock = DockStyle.Fill,
                    Enabled = true,
                    Name = "personUserControl",
                    Visible = true
                };

                _personUserControl.ButtonCancelClick += personUserControl_ButtonCancelClick;
                _personUserControl.ButtonSaveClick += personUserControl_ButtonSaveClick;
                _personUserControl.ButtonBadLoanClick += personUserControl_ButtonBadLoanClick;

                _personUserControl.ButtonAddProjectClick += personUserControl_ButtonAddProjectClick;

                _personUserControl.ViewProject += DisplayUserControl_ViewProject;
                _personUserControl.AddSelectedSaving += PersonUserControl_SavingSelected;
                _personUserControl.ViewSelectedSaving += PersonUserControl_ViewSelectedSaving;
                tabPageDetails.Controls.Add(_personUserControl);
                
                if (!ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
                {
                    _personUserControl.RemoveSavings();
                    panelSavingsContracts.Controls.Add(_personUserControl.PanelSavings);
                }
            }
            else if (pClientType == OClientTypes.Group)
            {
                _groupUserControl = new GroupUserControl(_group, pMdiParent)
                {
                    Dock = DockStyle.Fill,
                    Enabled = true,
                    Name = "groupUserControl",
                    Visible = true
                };
                _groupUserControl.ButtonCancelClick += GroupUserControl_ButtonCancelClick;
                _groupUserControl.ButtonSaveClick += GroupUserControl_ButtonSaveClick;
                _groupUserControl.ButtonBadClientClick += GroupUserControl_ButtonBadClientClick;

                _groupUserControl.ButtonAddProjectClick += GroupUserControl_ButtonAddProjectClick;
                _groupUserControl.ViewSelectedSaving += GroupUserControl_ViewSelectedSaving;
                _groupUserControl.AddSelectedSaving += GroupUserControl_AddSelectedSaving;

                _groupUserControl.ViewProject += DisplayUserControl_ViewProject;
                _groupUserControl.MembersChanged += MembersChanged;

                tabPageDetails.Controls.Add(_groupUserControl);
                
                if (!ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
                {
                    _groupUserControl.RemoveSavings();
                    panelSavingsContracts.Controls.Add(_groupUserControl.PanelSavings);
                    AddProject(null);
                }
            }
            else
            {
                _corporateUserControl = new CorporateUserControl(_corporate, pMdiParent)
                {
                    Dock = DockStyle.Fill,
                    Enabled = true,
                    Name = "corporateUserControl",
                    Visible = true
                };

                _corporateUserControl.ViewProject += DisplayUserControl_ViewProject;
                _corporateUserControl.ButtonCancel += CorporateUserControl_ButtonCancel;
                _corporateUserControl.ButtonAddProjectClick += corporateUserControl_ButtonAddProjectClick;
                _corporateUserControl.ButtonSaveClick += CorporateUserControl_ButtonSaveClick;
                _corporateUserControl.ViewSelectedSaving += CorporateUserControl_ViewSelectedSaving;
                _corporateUserControl.AddSelectedSaving += CorporateUserControl_AddSelectedSaving;

                if (_closeFormAfterSave)
                    _corporateUserControl.CloseCorporate += CorporateUserControl_Closed;

                tabPageDetails.Controls.Add(_corporateUserControl);
                
                if (!ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
                {
                    _corporateUserControl.RemoveSavings();
                    panelSavingsContracts.Controls.Add(_corporateUserControl.PanelSavings);
                    AddProject(null);
                    tabControlPerson.SelectedTab = tabPageCorporate;
                }
            }
        }

        private void InitializeCustomizableFields(OCustomizableFieldEntities entity, int? linkId, bool isActive)
        {
            if (entity == OCustomizableFieldEntities.Loan)
            {
                tabPageLoanCustomizableFields.Controls.Clear();
                _customizableLoanFieldsControl = new CustomizableFieldsControl(entity, linkId, isActive)
                {
                    Dock = DockStyle.Fill,
                    Enabled = true,
                    Name = "customizableLoanFieldsControl",
                    Visible = true
                };
                tabPageLoanCustomizableFields.Controls.Add(_customizableLoanFieldsControl);

            }
            else if (entity == OCustomizableFieldEntities.Savings)
            {
                tabPageSavingsCustomizableFields.Controls.Clear();
                _customizableSavingsFieldsControl = new CustomizableFieldsControl(entity, linkId, isActive)
                {
                    Dock = DockStyle.Fill,
                    Enabled = true,
                    Name = "customizableSavingsFieldsControl",
                    Visible = true
                };
                tabPageSavingsCustomizableFields.Controls.Add(_customizableSavingsFieldsControl);
            }
        }

        void GroupUserControl_AddSelectedSaving(object sender, EventArgs e)
        {
            InitializeTabPageSavingDetails((ISavingProduct)sender);
        }

        void GroupUserControl_ViewSelectedSaving(object sender, EventArgs e)
        {
            DisplaySaving((ISavingsContract)sender);
        }

        void CorporateUserControl_AddSelectedSaving(object sender, EventArgs e)
        {
            InitializeTabPageSavingDetails((ISavingProduct)sender);
        }

        void CorporateUserControl_ViewSelectedSaving(object sender, EventArgs e)
        {
            DisplaySaving((ISavingsContract)sender);
        }

        private void DisplaySavingProduct(ISavingProduct product)
        {
            lbInitialAmountMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                "Min ", product.InitialAmountMin.GetFormatedValue(product.Currency.UseCents),
                "Max ", product.InitialAmountMax.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            nudDownInitialAmount.Maximum = product.InitialAmountMax.Value;
            nudDownInitialAmount.Minimum = product.InitialAmountMin.Value;
            nudDownInitialAmount.Value = product.InitialAmountMin.Value;

            if (product.InterestRate.HasValue)
            {
                nudDownInterestRate.Enabled = false;
                lbInterestRateMinMax.Text = string.Format("{0} %", product.InterestRate * 100);
                nudDownInterestRate.Maximum = (decimal)product.InterestRate.Value * 100;
                nudDownInterestRate.Minimum = (decimal)product.InterestRate.Value * 100;
            }
            else
            {
                lbInterestRateMinMax.Text = string.Format("{0}{1} %\r\n{2}{3} %",
                    "Min ", product.InterestRateMin.Value * 100,
                    "Max ", product.InterestRateMax.Value * 100);
                nudDownInterestRate.Maximum = (decimal)product.InterestRateMax.Value * 100;
                nudDownInterestRate.Minimum = (decimal)product.InterestRateMin.Value * 100;
            }

            lbBalanceMinValue.Text = string.Format("{0} {1}", product.BalanceMin.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            lbBalanceMaxValue.Text = string.Format("{0} {1}", product.BalanceMax.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            lbWithdrawMinValue.Text = string.Format("{0} {1}", product.WithdrawingMin.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            lbWithdrawMaxValue.Text = string.Format("{0} {1}", product.WithdrawingMax.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            lbDepositMinValue.Text = string.Format("{0} {1}", product.DepositMin.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            lbDepositMaxValue.Text = string.Format("{0} {1}", product.DepositMax.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            labelSavingTransferMinValue.Text = string.Format("{0} {1}", product.TransferMin.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
            labelSavingTransferMaxValue.Text = string.Format("{0} {1}", product.TransferMax.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);

            if (product is SavingsBookProduct)
            {
                gbDepositInterest.Visible = false;
                gbInterest.Visible = true;

                lbWithdrawFees.Visible = true;
                nudWithdrawFees.Visible = true;
                lbWithdrawFeesMinMax.Visible = true;

                lbEntryFees.Visible = true;
                nudEntryFees.Visible = true;
                lbEntryFeesMinMax.Visible = true;

                lbTransferFees.Visible = true;
                nudTransferFees.Visible = true;
                nudIbtFee.Visible = true;
                lbTransferFeesMinMax.Visible = true;

                lbDepositFees.Visible = true;
                nudDepositFees.Visible = true;
                lbDepositFeesMinMax.Visible = true;

                lbChequeDepositFees.Visible = true;
                nudChequeDepositFees.Visible = true;
                lblChequeDepositFeesMinMax.Visible = true;

                lbCloseFees.Visible = true;
                nudCloseFees.Visible = true;
                lbCloseFeesMinMax.Visible = true;

                lbManagementFees.Visible = true;
                nudManagementFees.Visible = true;
                lbManagementFeesMinMax.Visible = true;

                lbOverdraftFees.Visible = true;
                nudOverdraftFees.Visible = true;
                lbOverdraftFeesMinMax.Visible = true;

                lbAgioFees.Visible = true;
                nudAgioFees.Visible = true;
                lbAgioFeesMinMax.Visible = true;

                lbReopenFees.Visible = true;
                nudReopenFees.Visible = true;
                lbReopenFeesMinMax.Visible = true;

                lbInterestAccrualValue.Text = MultiLanguageStrings.GetString(Ressource.FrmAddSavingProduct, ((SavingsBookProduct)product).InterestBase + ".Text");
                lbInterestPostingValue.Text = MultiLanguageStrings.GetString(Ressource.FrmAddSavingProduct, ((SavingsBookProduct)product).InterestFrequency + ".Text");
                lbInterestBasedOnValue.Text = ((SavingsBookProduct)product).CalculAmountBase.HasValue ? MultiLanguageStrings.GetString(Ressource.FrmAddSavingProduct, ((SavingsBookProduct)product).CalculAmountBase + ".Text") : "------";

                savingDepositToolStripMenuItem.Enabled = true;
                savingWithdrawToolStripMenuItem.Enabled = true;
                savingTransferToolStripMenuItem.Enabled = true;

                // entry fees
                nudEntryFees.DecimalPlaces = 0;
                nudEntryFees.Increment = 1;
                if (((SavingsBookProduct)product).EntryFees.HasValue)
                {
                    nudEntryFees.Enabled = false;
                    nudEntryFees.Minimum = ((SavingsBookProduct)product).EntryFees.Value;
                    nudEntryFees.Maximum = ((SavingsBookProduct)product).EntryFees.Value;
                    lbEntryFeesMinMax.Text = string.Format("{0} {1}", ((SavingsBookProduct)product).EntryFees.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
                }
                else
                {
                    nudEntryFees.Enabled = true;
                    nudEntryFees.Minimum = ((SavingsBookProduct)product).EntryFeesMin.Value;
                    nudEntryFees.Maximum = ((SavingsBookProduct)product).EntryFeesMax.Value;
                    lbEntryFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                        "Min ", ((SavingsBookProduct)product).EntryFeesMin.GetFormatedValue(product.Currency.UseCents),
                        "Max ", ((SavingsBookProduct)product).EntryFeesMax.GetFormatedValue(product.Currency.UseCents),
                        product.Currency.Code);
                }

                // withdraw fees
                if (((SavingsBookProduct)product).WithdrawFeesType == OSavingsFeesType.Flat)
                {
                    nudWithdrawFees.DecimalPlaces = 0;
                    nudWithdrawFees.Increment = 1;
                    if (((SavingsBookProduct)product).FlatWithdrawFees.HasValue)
                    {
                        nudWithdrawFees.Enabled = false;
                        nudWithdrawFees.Minimum = ((SavingsBookProduct)product).FlatWithdrawFees.Value;
                        nudWithdrawFees.Maximum = ((SavingsBookProduct)product).FlatWithdrawFees.Value;
                        lbWithdrawFeesMinMax.Text = string.Format("{0} {1}", ((SavingsBookProduct)product).FlatWithdrawFees.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
                    }
                    else
                    {
                        nudWithdrawFees.Enabled = true;
                        nudWithdrawFees.Minimum = ((SavingsBookProduct)product).FlatWithdrawFeesMin.Value;
                        nudWithdrawFees.Maximum = ((SavingsBookProduct)product).FlatWithdrawFeesMax.Value;
                        lbWithdrawFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                            "Min ", ((SavingsBookProduct)product).FlatWithdrawFeesMin.GetFormatedValue(product.Currency.UseCents),
                            "Max ", ((SavingsBookProduct)product).FlatWithdrawFeesMax.GetFormatedValue(product.Currency.UseCents),
                            product.Currency.Code);
                    }
                }
                else
                {
                    nudWithdrawFees.DecimalPlaces = 4;
                    nudWithdrawFees.Increment = 0.0001m;
                    if (((SavingsBookProduct)product).RateWithdrawFees.HasValue)
                    {
                        nudWithdrawFees.Enabled = false;
                        nudWithdrawFees.Minimum = (decimal)((SavingsBookProduct)product).RateWithdrawFees.Value * 100;
                        nudWithdrawFees.Maximum = (decimal)((SavingsBookProduct)product).RateWithdrawFees.Value * 100;
                        lbWithdrawFeesMinMax.Text = string.Format("{0} {1}", (((SavingsBookProduct)product).RateWithdrawFees * 100), "%");
                    }
                    else
                    {
                        nudWithdrawFees.Enabled = true;
                        nudWithdrawFees.Minimum = (decimal)((SavingsBookProduct)product).RateWithdrawFeesMin.Value * 100;
                        nudWithdrawFees.Maximum = (decimal)((SavingsBookProduct)product).RateWithdrawFeesMax.Value * 100;
                        lbWithdrawFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                            "Min ", (((SavingsBookProduct)product).RateWithdrawFeesMin * 100),
                            "Max ", (((SavingsBookProduct)product).RateWithdrawFeesMax * 100),
                            "%");
                    }
                }

                // Inter-branch tansfer fee
                SavingsBookProduct p = (SavingsBookProduct)product;
                Fee fee = p.InterBranchTransferFee;
                nudIbtFee.DecimalPlaces = fee.IsFlat ? 0 : 2;
                nudIbtFee.Increment = fee.IsFlat ? 1 : 0.01m;
                nudIbtFee.Enabled = !fee.Value.HasValue;
                nudIbtFee.Minimum = fee.GetMin();
                nudIbtFee.Maximum = fee.GetMax();
                if (fee.IsRange)
                {
                    string min = fee.GetMinFormatted(p.Currency);
                    string max = fee.GetMaxFormatted(p.Currency);
                    const string mask = "Min {0}\r\nMax {1}";
                    lblIbtFeeMinMax.Text = string.Format(mask, min, max);
                }
                else
                {
                    lblIbtFeeMinMax.Text = fee.GetValueFormatted(p.Currency);
                }

                if (((SavingsBookProduct)product).TransferFeesType == OSavingsFeesType.Flat)
                {
                    nudTransferFees.DecimalPlaces = 0;
                    nudTransferFees.Increment = 1;
                    if (((SavingsBookProduct)product).FlatTransferFees.HasValue)
                    {
                        nudTransferFees.Enabled = false;
                        nudTransferFees.Minimum = ((SavingsBookProduct)product).FlatTransferFees.Value;
                        nudTransferFees.Maximum = ((SavingsBookProduct)product).FlatTransferFees.Value;
                        lbTransferFeesMinMax.Text = string.Format("{0} {1}", ((SavingsBookProduct)product).FlatTransferFees.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
                    }
                    else
                    {
                        nudTransferFees.Enabled = true;
                        nudTransferFees.Minimum = ((SavingsBookProduct)product).FlatTransferFeesMin.Value;
                        nudTransferFees.Maximum = ((SavingsBookProduct)product).FlatTransferFeesMax.Value;
                        lbTransferFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                            "Min ", ((SavingsBookProduct)product).FlatTransferFeesMin.GetFormatedValue(product.Currency.UseCents),
                            "Max ", ((SavingsBookProduct)product).FlatTransferFeesMax.GetFormatedValue(product.Currency.UseCents),
                            product.Currency.Code);
                    }
                }
                else
                {
                    nudTransferFees.DecimalPlaces = 4;
                    nudTransferFees.Increment = 0.0001m;
                    if (((SavingsBookProduct)product).RateTransferFees.HasValue)
                    {
                        nudTransferFees.Enabled = false;
                        nudTransferFees.Minimum = (decimal)((SavingsBookProduct)product).RateTransferFees.Value * 100;
                        nudTransferFees.Maximum = (decimal)((SavingsBookProduct)product).RateTransferFees.Value * 100;
                        lbTransferFeesMinMax.Text = string.Format("{0} {1}", (((SavingsBookProduct)product).RateTransferFees * 100), "%");
                    }
                    else
                    {
                        nudTransferFees.Enabled = true;
                        nudTransferFees.Minimum = (decimal)((SavingsBookProduct)product).RateTransferFeesMin.Value * 100;
                        nudTransferFees.Maximum = (decimal)((SavingsBookProduct)product).RateTransferFeesMax.Value * 100;
                        lbTransferFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                            "Min ", (((SavingsBookProduct)product).RateTransferFeesMin * 100),
                            "Max ", (((SavingsBookProduct)product).RateTransferFeesMax * 100),
                            "%");
                    }
                }

                //Cash Deposit fees
                nudDepositFees.DecimalPlaces = 0;
                nudDepositFees.Increment = 1;
                if (((SavingsBookProduct)product).DepositFees.HasValue)
                {
                    nudDepositFees.Enabled = false;
                    nudDepositFees.Minimum = ((SavingsBookProduct)product).DepositFees.Value;
                    nudDepositFees.Maximum = ((SavingsBookProduct)product).DepositFees.Value;
                    lbDepositFeesMinMax.Text = string.Format("{0} {1}",
                                                             ((SavingsBookProduct)product).DepositFees.GetFormatedValue(product.Currency.UseCents),
                                                             product.Currency.Code);
                }
                else
                {
                    nudDepositFees.Enabled = true;
                    nudDepositFees.Minimum = ((SavingsBookProduct)product).DepositFeesMin.Value;
                    nudDepositFees.Maximum = ((SavingsBookProduct)product).DepositFeesMax.Value;
                    lbDepositFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                                                             "Min ", ((SavingsBookProduct)product).DepositFeesMin.GetFormatedValue(product.Currency.UseCents),
                                                             "Max ", ((SavingsBookProduct)product).DepositFeesMax.GetFormatedValue(product.Currency.UseCents),
                                                             product.Currency.Code);
                }

                //Cheque Deposit fees
                nudChequeDepositFees.DecimalPlaces = 0;
                nudChequeDepositFees.Increment = 1;
                if (((SavingsBookProduct)product).ChequeDepositFees.HasValue)
                {
                    nudChequeDepositFees.Enabled = false;
                    nudChequeDepositFees.Maximum = ((SavingsBookProduct)product).ChequeDepositFees.Value;
                    nudChequeDepositFees.Minimum = ((SavingsBookProduct)product).ChequeDepositFees.Value;
                    lblChequeDepositFeesMinMax.Text = string.Format("{0} {1}",
                                                             ((SavingsBookProduct)product).ChequeDepositFees.GetFormatedValue(product.Currency.UseCents),
                                                             product.Currency.Code);
                }
                else
                {
                    nudChequeDepositFees.Enabled = true;
                    nudChequeDepositFees.Minimum = ((SavingsBookProduct)product).ChequeDepositFeesMin.Value;
                    nudChequeDepositFees.Maximum = ((SavingsBookProduct)product).ChequeDepositFeesMax.Value;
                    lblChequeDepositFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                                                             "Min ", ((SavingsBookProduct)product).ChequeDepositFeesMin.GetFormatedValue(product.Currency.UseCents),
                                                             "Max ", ((SavingsBookProduct)product).ChequeDepositFeesMax.GetFormatedValue(product.Currency.UseCents),
                                                             product.Currency.Code);
                }

                // Close fees
                nudCloseFees.DecimalPlaces = 0;
                nudCloseFees.Increment = 1;
                if (((SavingsBookProduct)product).CloseFees.HasValue)
                {
                    nudCloseFees.Enabled = false;
                    nudCloseFees.Minimum = ((SavingsBookProduct)product).CloseFees.Value;
                    nudCloseFees.Maximum = ((SavingsBookProduct)product).CloseFees.Value;
                    lbCloseFeesMinMax.Text = string.Format("{0} {1}",
                        ((SavingsBookProduct)product).CloseFees.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
                }
                else
                {
                    nudCloseFees.Enabled = true;
                    nudCloseFees.Minimum = ((SavingsBookProduct)product).CloseFeesMin.Value;
                    nudCloseFees.Maximum = ((SavingsBookProduct)product).CloseFeesMax.Value;
                    lbCloseFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                        "Min ", ((SavingsBookProduct)product).CloseFeesMin.GetFormatedValue(product.Currency.UseCents),
                        "Max ", ((SavingsBookProduct)product).CloseFeesMax.GetFormatedValue(product.Currency.UseCents),
                        product.Currency.Code);
                }

                // Management fees
                nudManagementFees.DecimalPlaces = 0;
                nudManagementFees.Increment = 1;
                if (((SavingsBookProduct)product).ManagementFees.HasValue)
                {
                    nudManagementFees.Enabled = false;
                    nudManagementFees.Minimum = ((SavingsBookProduct)product).ManagementFees.Value;
                    nudManagementFees.Maximum = ((SavingsBookProduct)product).ManagementFees.Value;
                    lbManagementFeesMinMax.Text = string.Format("{0} {1}",
                        ((SavingsBookProduct)product).ManagementFees.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
                }
                else
                {
                    nudManagementFees.Enabled = true;
                    nudManagementFees.Minimum = ((SavingsBookProduct)product).ManagementFeesMin.Value;
                    nudManagementFees.Maximum = ((SavingsBookProduct)product).ManagementFeesMax.Value;
                    lbManagementFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                        "Min ", ((SavingsBookProduct)product).ManagementFeesMin.GetFormatedValue(product.Currency.UseCents),
                        "Max ", ((SavingsBookProduct)product).ManagementFeesMax.GetFormatedValue(product.Currency.UseCents),
                        product.Currency.Code);
                }

                // Overdraft fees
                nudOverdraftFees.DecimalPlaces = 0;
                nudOverdraftFees.Increment = 1;
                if (((SavingsBookProduct)product).OverdraftFees.HasValue)
                {
                    nudOverdraftFees.Enabled = false;
                    nudOverdraftFees.Minimum = ((SavingsBookProduct)product).OverdraftFees.Value;
                    nudOverdraftFees.Maximum = ((SavingsBookProduct)product).OverdraftFees.Value;
                    lbOverdraftFeesMinMax.Text = string.Format("{0} {1}",
                        ((SavingsBookProduct)product).OverdraftFees.GetFormatedValue(product.Currency.UseCents), product.Currency.Code);
                }
                else
                {
                    nudOverdraftFees.Enabled = true;
                    nudOverdraftFees.Minimum = ((SavingsBookProduct)product).OverdraftFeesMin.Value;
                    nudOverdraftFees.Maximum = ((SavingsBookProduct)product).OverdraftFeesMax.Value;
                    lbOverdraftFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                        "Min ", ((SavingsBookProduct)product).OverdraftFeesMin.GetFormatedValue(product.Currency.UseCents),
                        "Max ", ((SavingsBookProduct)product).OverdraftFeesMax.GetFormatedValue(product.Currency.UseCents),
                        product.Currency.Code);
                }

                // Agio fees
                nudAgioFees.DecimalPlaces = 4;
                nudAgioFees.Increment = 0.0001m;
                if (((SavingsBookProduct)product).AgioFees.HasValue)
                {
                    nudAgioFees.Enabled = false;
                    nudAgioFees.Minimum = (decimal)((SavingsBookProduct)product).AgioFees.Value * 100;
                    nudAgioFees.Maximum = (decimal)((SavingsBookProduct)product).AgioFees.Value * 100;
                    lbAgioFeesMinMax.Text = string.Format("{0} {1}", ((SavingsBookProduct)product).AgioFees * 100, "%");
                }
                else
                {
                    nudAgioFees.Enabled = true;
                    nudAgioFees.Minimum = (decimal)((SavingsBookProduct)product).AgioFeesMin.Value * 100;
                    nudAgioFees.Maximum = (decimal)((SavingsBookProduct)product).AgioFeesMax.Value * 100;
                    lbAgioFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                        "Min ", ((SavingsBookProduct)product).AgioFeesMin * 100,
                        "Max ", ((SavingsBookProduct)product).AgioFeesMax * 100, "%");
                }

                //Reopen fees
                nudReopenFees.DecimalPlaces = 0;
                nudReopenFees.Increment = 1;

                if (((SavingsBookProduct)product).ReopenFees.HasValue)
                {
                    nudReopenFees.Enabled = false;
                    nudReopenFees.Minimum = ((SavingsBookProduct)product).ReopenFees.Value;
                    nudReopenFees.Maximum = ((SavingsBookProduct)product).ReopenFees.Value;
                    lbReopenFeesMinMax.Text = string.Format("{0} {1}",
                                                             ((SavingsBookProduct)product).ReopenFees.GetFormatedValue(product.Currency.UseCents),
                                                             product.Currency.Code);
                }
                else
                {
                    nudReopenFees.Enabled = true;
                    nudReopenFees.Minimum = ((SavingsBookProduct)product).ReopenFeesMin.Value;
                    nudReopenFees.Maximum = ((SavingsBookProduct)product).ReopenFeesMax.Value;
                    lbReopenFeesMinMax.Text = string.Format("{0}{1} {4}\r\n{2}{3} {4}",
                                                             "Min ", ((SavingsBookProduct)product).ReopenFeesMin.GetFormatedValue(product.Currency.UseCents),
                                                             "Max ", ((SavingsBookProduct)product).ReopenFeesMax.GetFormatedValue(product.Currency.UseCents),
                                                             product.Currency.Code);
                }

                if (((SavingsBookProduct)product).UseTermDeposit)
                {
                    nudNumberOfPeriods.Enabled = true;
                    btSearchContract2.Enabled = true;
                    cmbRollover2.Enabled = true;
                    nudNumberOfPeriods.Minimum = (decimal)((SavingsBookProduct)product).TermDepositPeriodMin;
                    nudNumberOfPeriods.Maximum = (decimal)((SavingsBookProduct)product).TermDepositPeriodMax;
                    nudNumberOfPeriods.Value = nudNumberOfPeriods.Minimum;
                    tbTargetAccount2.ResetText();
                }

            }

        }

        public void DisplaySaving(int pId, IClient client)
        {
            ISavingsContract saving;

            if (_oClientType == OClientTypes.Person)
                saving = _person.Savings.FirstOrDefault(item => item.Id == pId);
            else if (_oClientType == OClientTypes.Corporate)
                saving = _corporate.Savings.FirstOrDefault(item => item.Id == pId);
            else if (_oClientType == OClientTypes.Group)
                saving = _group.Savings.FirstOrDefault(item => item.Id == pId);
            else
                throw new Exception();

            DisplaySaving(saving);
            var loans = new List<Loan>();
            if (client.Projects != null)
                foreach (var project in client.Projects)
                {
                    foreach (var credit in project.Credits)
                    {
                        loans.Add(credit);
                    }
                }
            DisplayContracts(loans);

        }

        private void DisplaySaving(ISavingsContract saving)
        {
            saving = SavingServices.GetSaving(saving.Id);

            ((SavingBookContract)saving).Loans = SavingServices.SelectLoansBySavingsId(saving.Id);

            if (!tabControlPerson.TabPages.Contains(tabPageContracts))
            {
                tabControlPerson.TabPages.Add(tabPageContracts);
                panelLoansContracts.Controls.Add(pnlLoans);
            }

            tabControlPerson.TabPages.Remove(tabPageSavingDetails);
            tabControlPerson.TabPages.Add(tabPageSavingDetails);

            _saving = (SavingBookContract)saving;
            DisplaySavingProduct(_saving.Product);
            InitializeTabPageTermDeposit();
            nudDownInterestRate.Enabled = false;
            nudDownInitialAmount.Enabled = false;
            nudWithdrawFees.Enabled = false;
            nudEntryFees.Enabled = false;
            nudTransferFees.Enabled = false;
            nudIbtFee.Enabled = false;
            nudDepositFees.Enabled = false;
            nudChequeDepositFees.Enabled = false;
            nudCloseFees.Enabled = false;
            nudManagementFees.Enabled = false;
            nudOverdraftFees.Enabled = false;
            nudAgioFees.Enabled = false;
            nudReopenFees.Enabled = false;
            btSavingsUpdate.Visible = false;
            nudNumberOfPeriods.Enabled = false;
            btSearchContract2.Enabled = false;
            cmbRollover2.Enabled = false;
            cmbSavingsOfficer.Enabled = false;
            cmbSavingsOfficer.Items.Add(User.CurrentUser);
            foreach (User subordinate in User.CurrentUser.Subordinates)
            {
                cmbSavingsOfficer.Items.Add(subordinate);
            }
            int index = -1;
            for (int i = 0; i < cmbSavingsOfficer.Items.Count; i++)
            {
                User u = (User)cmbSavingsOfficer.Items[i];
                if (u.Id != saving.SavingsOfficer.Id) continue;

                index = i;
                break;
            }
            cmbSavingsOfficer.SelectedIndex = index;
            tabControlPerson.SelectedTab = tabPageSavingDetails;
            Text = string.Format("{0} - {1}", _title, _saving.Code);

            groupBoxSaving.Name += string.Format(" {0}", _saving.Product.Name);
            groupBoxSaving.Text = string.Format("{0} : {1}",
                MultiLanguageStrings.GetString(Ressource.ClientForm,
                _saving is SavingBookContract ? "SavingsBook.Text" : "CompulsorySavings.Text"),
                MultiLanguageStrings.GetString(Ressource.ClientForm, "Savings" + _saving.Status + ".Text"));

            switch (_saving.Status)
            {
                case OSavingsStatus.Pending:
                    {
                        groupBoxSaving.ForeColor = Color.FromArgb(246, 137, 56);
                        pnlSavingsButtons.Enabled = false;
                        buttonFirstDeposit.Visible = true;
                        buttonCloseSaving.Visible = false;
                        buttonReopenSaving.Visible = false;
                        break;
                    }
                case OSavingsStatus.Active:
                    {
                        groupBoxSaving.ForeColor = Color.FromArgb(61, 153, 57);
                        pnlSavingsButtons.Enabled = true;
                        buttonFirstDeposit.Visible = false;
                        buttonCloseSaving.Visible = true;
                        buttonReopenSaving.Visible = false;
                        break;
                    }
                case OSavingsStatus.Closed:
                    {
                        groupBoxSaving.ForeColor = Color.Red;
                        pnlSavingsButtons.Enabled = true;
                        buttonSavingsOperations.Enabled = false;
                        buttonFirstDeposit.Visible = false;
                        buttonCloseSaving.Visible = false;
                        buttonReopenSaving.Visible = true;
                        break;
                    }
            }

            tBSavingCode.Text = _saving.Code;

            DisplaySavingEvent(_saving);
            DisplaySavingLoans(_saving);

            buttonSaveSaving.Visible = false;

            InitSavingsBookPrintButton();


            if (saving != null && saving.Id > 0)
            {
                InitializeCustomizableFields(OCustomizableFieldEntities.Savings, saving.Id, true);
                //InitialDoclistSaving();
            }
            else
            {
                InitializeCustomizableFields(OCustomizableFieldEntities.Savings, null, false);
                //dlcSaving.Clear();
            }

            LoadSavingsExtensions();

        }

        private void DisplaySavingLoans(ISavingsContract saving)
        {
            if (saving is SavingBookContract)
            {
                if (((SavingBookContract)saving).Loans != null)
                {
                    if (((SavingBookContract)saving).Loans.Count > 0)
                    {
                        olvColumnStatus.AspectToStringConverter = delegate(object value)
                        {
                            if (value.ToString().Length > 0)
                            {
                                string status = GetString(value.ToString() + ".Text");
                                return status;
                            }
                            return null;
                        };

                        olvColumnAmount.AspectToStringConverter = delegate(object value)
                                                                      {
                                                                          if (value.ToString().Length > 0)
                                                                          {
                                                                              OCurrency amount = (OCurrency)value;
                                                                              return amount.GetFormatedValue(true);
                                                                          }
                                                                          return null;
                                                                      };

                        olvColumnStratDate.AspectToStringConverter = delegate(object value)
                                                                         {
                                                                             if (value.ToString().Length > 0)
                                                                             {
                                                                                 return
                                                                                     ((DateTime)value).
                                                                                         ToShortDateString();
                                                                             }
                                                                             return null;
                                                                         };

                        olvColumnCreationDate.AspectToStringConverter = delegate(object value)
                                                                            {
                                                                                if (value.ToString().Length > 0)
                                                                                {
                                                                                    return
                                                                                        ((DateTime)value).
                                                                                            ToShortDateString();
                                                                                }
                                                                                return null;
                                                                            };

                        olvColumnCloseDate.AspectToStringConverter = delegate(object value)
                                                                         {
                                                                             if (value.ToString().Length > 0)
                                                                             {
                                                                                 return
                                                                                     ((DateTime)value).
                                                                                         ToShortDateString();
                                                                             }
                                                                             return null;
                                                                         };
                        olvLoans.SetObjects(((SavingBookContract)saving).Loans);
                        tabControlSavingsDetails.SelectedIndex = 0;
                        return;
                    }
                }
            }

            //tabcSavingsDettails.TabPages.Remove(tabPageLoans);
        }

        private void PersonUserControl_ViewSelectedSaving(object sender, EventArgs e)
        {
            DisplaySaving((ISavingsContract)sender);
        }

        private void CorporateUserControl_ButtonCancel(object sender, EventArgs e)
        {
            Close();
        }

        private void CorporateUserControl_Closed(object sender, EventArgs e)
        {
            Close();
        }

        private void GroupUserControl_ButtonAddProjectClick(object sender, EventArgs e)
        {
            AddProject(null);
        }

        private void GroupUserControl_ButtonBadClientClick(object sender, EventArgs e)
        {
            _group = _groupUserControl.Group;
            InitializeTitle(_group.Name);
        }

        private void personUserControl_ButtonSaveClick(object sender, EventArgs e)
        {
            if (_personUserControl.PersonSaved)
            {
                _person = _personUserControl.Person;
                _client = _person;
                if (_mdiParent != null)
                    ((LotrasmicMainWindowForm)_mdiParent).SetInfoMessage(string.Format("Person {0} {1} saved", _person.FirstName, _person.LastName));
                InitializeTitle(string.Format("{1} {0}", _person.FirstName, _person.LastName));
                if (_closeFormAfterSave)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                if (!tabControlPerson.TabPages.Contains(tabPageContracts))
                {
                    tabControlPerson.TabPages.Add(tabPageContracts);
                    panelLoansContracts.Controls.Add(pnlLoans);
                }

            }
            else
            {
                _person = null;
                //DialogResult = DialogResult.Cancel;
            }
        }

        private void GroupUserControl_ButtonSaveClick(object sender, EventArgs e)
        {
            if (_groupUserControl.GroupSaved)
            {
                _group = _groupUserControl.Group;
                _client = _group;
                ((LotrasmicMainWindowForm)_mdiParent).SetInfoMessage(string.Format("Group {0} saved", _group.Name));
                InitializeTitle(_group.Name);
                if (!tabControlPerson.TabPages.Contains(tabPageContracts))
                {
                    tabControlPerson.TabPages.Add(tabPageContracts);
                    panelLoansContracts.Controls.Add(pnlLoans);
                }
            }
            else
                _group = null;
        }

        private void CorporateUserControl_ButtonSaveClick(object sender, EventArgs e)
        {
            if (_corporateUserControl.CorporateSaved)
            {
                _corporate = _corporateUserControl.Corporate;
                _client = _corporate;
                ((LotrasmicMainWindowForm)_mdiParent).SetInfoMessage(string.Format("Corporate {0} saved", _corporate.Name));
                InitializeTitle(_corporate.Name);
                if (!tabControlPerson.TabPages.Contains(tabPageContracts))
                {
                    tabControlPerson.TabPages.Add(tabPageContracts);
                    panelLoansContracts.Controls.Add(pnlLoans);
                }
            }
        }

        private void GroupUserControl_ButtonCancelClick(object sender, EventArgs e)
        {
            _group = null;
            Close();
        }

        private void InitializeProjectCombobox()
        {
            InitializeComboboxProjectJuridicStatus();
            InitializeComboboxProjectFiscalStatus();
            InitializeComboboxProjectAffiliation();
            InitializeComboboxProjectFinancialPlanType();
        }

        private void DisplaySelectedProject(Project pProject)
        {
            InitializeProjectCombobox();
            Text = _title;
            if (pProject.Name != "Not Set")
                Text += " - " + pProject.Name;

            _followUpList = pProject.FollowUps;
            buttonProjectSave.Text = pProject.Id != 0 ? MultiLanguageStrings.GetString(Ressource.ClientForm, "UpdateProject.Text") : MultiLanguageStrings.GetString(Ressource.ClientForm, "SaveProject.Text");

            tabPageProject.Text = pProject.Id != 0 ? string.Format(MultiLanguageStrings.GetString(Ressource.ClientForm, "Project.Text") + " - [{0}]", _project.Name) : MultiLanguageStrings.GetString(Ressource.ClientForm, "Project.Text");

            textBoxProjectName.Text = pProject.Name;
            textBoxProjectCode.Text = pProject.Code;
            textBoxProjectAim.Text = pProject.Aim;
            textBoxProjectAbilities.Text = pProject.Abilities;
            textBoxProjectExperience.Text = pProject.Experience;
            textBoxProjectMarket.Text = pProject.Market;
            textBoxProjectConcurrence.Text = pProject.Concurrence;
            textBoxProjectPurpose.Text = pProject.Purpose;
            dateTimePickerProjectBeginDate.Value = pProject.BeginDate == DateTime.MinValue
                                                       ? TimeProvider.Today
                                                       : pProject.BeginDate;

            tBProjectCorporateName.Text = pProject.CorporateName;
            tBProjectCorporateSIRET.Text = pProject.CorporateSIRET;
            cBProjectJuridicStatus.Text = pProject.CorporateJuridicStatus;
            cBProjectFiscalStatus.Text = pProject.CorporateFiscalStatus;
            cBProjectAffiliation.Text = pProject.CorporateRegistre;
            tBProjectCA.Text = pProject.CorporateCA.HasValue ? pProject.CorporateCA.GetFormatedValue(true) : "";
            numericUpDownProjectJobs.Value = pProject.CorporateNbOfJobs.HasValue ? pProject.CorporateNbOfJobs.Value : 0;
            cBProjectFinancialPlanType.Text = pProject.CorporateFinancialPlanType;
            tBProjectFinancialPlanAmount.Text = pProject.CorporateFinancialPlanAmount.HasValue ? pProject.CorporateFinancialPlanAmount.GetFormatedValue(true) : "";
            tBProjectFinancialPlanTotal.Text = pProject.CorporateFinancialPlanTotalAmount.HasValue ? pProject.CorporateFinancialPlanTotalAmount.GetFormatedValue(true) : "";

            projectAddressUserControl.ZipCode = pProject.ZipCode;
            projectAddressUserControl.HomeType = pProject.HomeType;
            projectAddressUserControl.Email = pProject.Email;
            projectAddressUserControl.District = pProject.District;
            projectAddressUserControl.City = pProject.City;
            projectAddressUserControl.Comments = pProject.Address;
            projectAddressUserControl.HomePhone = pProject.HomePhone;
            projectAddressUserControl.PersonalPhone = pProject.PersonalPhone;

            DisplayContracts(pProject.Credits);
            DisplayFollowUps(_followUpList);
        }

        private void DisplayContractsGuarantee(IEnumerable<Guarantee> pGuarantees)
        {
            foreach (Guarantee guarantee in pGuarantees)
            {
                if (!guarantee.Closed)
                {
                    if (guarantee is Guarantee)
                    {
                        ListViewItem item = new ListViewItem("G");
                        item.Tag = guarantee;
                        item.SubItems.Add(guarantee.Code);
                        item.SubItems.Add(guarantee.ContractStatus.ToString());
                        item.SubItems.Add(guarantee.Amount.GetFormatedValue(_credit.UseCents));
                        item.SubItems.Add(guarantee.FundingLine.Currency.Code);
                        item.SubItems.Add("-");
                        item.SubItems.Add("-");
                        item.SubItems.Add(guarantee.CreationDate.ToShortDateString());
                        item.SubItems.Add(guarantee.StartDate.ToShortDateString());
                        item.SubItems.Add(guarantee.CloseDate.ToShortDateString());
                        lvContracts.Items.Add(item);
                        continue;
                    }
                    else
                    {
                        ListViewItem item = new ListViewItem(guarantee.Code);
                        lvContracts.Items.Add(item);
                    }
                }
            }
        }

        private void FillProjectLoans(List<Project> pProjects, string Name)
        {
            List<Loan> _credits = new List<Loan>();

            if (pProjects.Count > 0)
            {
                foreach (Project proj in pProjects)
                {
                    foreach (Loan _loan in proj.Credits)
                    {
                        _credits.Add(_loan);
                    }
                }
                DisplayContracts(_credits);

                _project = pProjects[0];
                //tabControlPerson.SelectedTab = tabPageProjectLoans;
                tabControlPerson.SelectedTab = tabPageContracts;
            }
            else
            {
                if (Name != null)
                {
                    _project = new Project();
                    textBoxProjectName.Text = "NotSet";
                    textBoxProjectAim.Text = "NotSet";
                    dateTimePickerProjectBeginDate.Value = DateTime.Now;
                    SaveProject();
                    tabControlPerson.SelectedTab = tabPageDetails;
                }
            }
        }

        private void AddProject(Project pProject)
        {
            if (ServicesProvider.GetInstance().GetGeneralSettings().UseProjects)
            {
                tabControlPerson.TabPages.Remove(tabPageProject);
                tabControlPerson.TabPages.Remove(tabPageLoansDetails);
                tabControlPerson.TabPages.Remove(tabPageAdvancedSettings);
                tabControlPerson.TabPages.Remove(tabPageLoanRepayment);

                tabControlPerson.TabPages.Remove(tabPageSavingDetails);
                tabControlPerson.TabPages.Remove(tabPageLoanGuarantees);

                tabControlPerson.TabPages.Add(tabPageProject);
                tabControlPerson.SelectedTab = tabPageProject;
                if ((pProject == null))
                {
                    _project = new Project();
                    pProject = _project;
                }

                DisplaySelectedProject(pProject);
            }
            else
            {
                tabControlPerson.TabPages.Remove(tabPageProject);

                bool IsTabContractsExists = false;

                foreach (TabPage tab in tabControlPerson.TabPages)
                {
                    if (tab == tabPageContracts)
                        IsTabContractsExists = true;
                }

                if (!IsTabContractsExists && _client != null)
                {
                    tabControlPerson.TabPages.Add(tabPageContracts);
                    panelLoansContracts.Controls.Add(pnlLoans);

                    if (_project != null)
                    {
                        tabControlPerson.SelectedTab = tabPageContracts;
                    }

                    if (_personUserControl != null)
                    {
                        FillProjectLoans(_personUserControl.Person.Projects, _personUserControl.Person.FirstName);
                    }

                    if (_groupUserControl != null)
                    {
                        FillProjectLoans(_groupUserControl.Group.Projects, _groupUserControl.Group.Name);
                    }

                    if (_corporateUserControl != null)
                    {
                        FillProjectLoans(_corporateUserControl.Corporate.Projects, _corporateUserControl.Corporate.Name);
                    }
                }
            }
        }

        private void DisplayContracts(IEnumerable<Loan> loans)
        {
            OCurrency totalOlb = 0;
            OCurrency totalOlbInPivot = 0;
            OCurrency totalAmount = 0;
            OCurrency totalAmountInPivot = 0;
            ExchangeRate latestExchangeRate;
            ExchangeRate customExchangeRate;
            bool usedCents = false;
            ApplicationSettings dataParam = ApplicationSettings.GetInstance(string.Empty);
            int decimalPlaces = dataParam.InterestRateDecimalPlaces;

            string currencyCodeHolder = null; // to detect, if credits are in different currencies
            bool multiCurrency = false;

            lvContracts.Items.Clear();

            foreach (Loan credit in loans)
            {

                // it will be done for the first credit
                if (currencyCodeHolder == null) currencyCodeHolder = credit.Product.Currency.Code;

                //if not the first
                if (credit.Product.Currency.Code != currencyCodeHolder) multiCurrency = true;
                currencyCodeHolder = credit.Product.Currency.Code;


                //In case, if there are contracts in different currencies, total values of OLB and Credit amounts are displayed in pivot currency
                //For OLB, we must use current exchange rate to calculate in single currency.But for credit amounts, we must use exchange rates
                //recorded at disbursement date.

                latestExchangeRate =
                    ServicesProvider.GetInstance().GetAccountingServices().FindLatestExchangeRate(TimeProvider.Today,
                                                                                            credit.Product.Currency);
                customExchangeRate =
                    ServicesProvider.GetInstance().GetAccountingServices().FindLatestExchangeRate(
                        credit.StartDate, credit.Product.Currency);

                var item = new ListViewItem("C") { Tag = credit };
                item.SubItems.Add(credit.Code);

                item.SubItems.Add(MultiLanguageStrings.GetString(Ressource.ClientForm, credit.ContractStatus + ".Text"));

                item.SubItems.Add(credit.Amount.GetFormatedValue(credit.UseCents));
                if (credit.ContractStatus == OContractStatus.Abandoned || credit.ContractStatus == OContractStatus.Refused)
                    item.SubItems.Add(credit.UseCents ? "0.00" : "0");
                else
                    item.SubItems.Add(credit.CalculateActualOlb().GetFormatedValue(credit.UseCents));
                item.SubItems.Add(credit.Product.Currency.Code);
                item.SubItems.Add(Math.Round(credit.InterestRate * 100, decimalPlaces).ToString());
                item.SubItems.Add(credit.InstallmentType.Name);
                item.SubItems.Add(credit.NbOfInstallments.ToString());
                item.SubItems.Add(credit.CreationDate.ToShortDateString());
                item.SubItems.Add(credit.StartDate.ToShortDateString());
                item.SubItems.Add(credit.CloseDate.ToShortDateString());

                if (credit.ContractStatus != OContractStatus.Abandoned && credit.ContractStatus != OContractStatus.Refused)
                {

                    if (credit.ContractStatus != OContractStatus.Closed)
                    {

                        totalAmount += credit.Amount;
                        totalAmountInPivot += customExchangeRate.Rate == 0
                                                  ? 0
                                                  : credit.Amount / customExchangeRate.Rate;

                        totalOlb += credit.CalculateActualOlb();
                        totalOlbInPivot += latestExchangeRate.Rate == 0
                                               ? 0
                                               : credit.CalculateActualOlb() /
                                                 latestExchangeRate.Rate;

                        credit.CalculateActualOlb();
                    }
                    else if (credit.ContractStatus == OContractStatus.Closed)
                    {
                        totalAmount += credit.Amount;
                        totalAmountInPivot += customExchangeRate.Rate == 0
                                                  ? 0
                                                  : credit.Amount / customExchangeRate.Rate;
                    }
                }

                lvContracts.Items.Add(item);

                if (credit.UseCents)
                    usedCents = credit.UseCents;
            }

            var totalItem = new ListViewItem("");
            totalItem.Font = new Font(totalItem.Font, FontStyle.Bold);
            totalItem.SubItems.Add("");
            totalItem.SubItems.Add("");

            int midPoint = usedCents ? 2 : 0;

            totalAmountInPivot = Math.Round(totalAmountInPivot.Value, midPoint, MidpointRounding.AwayFromZero);
            totalOlbInPivot = Math.Round(totalOlbInPivot.Value, midPoint, MidpointRounding.AwayFromZero);

            if (multiCurrency)
            {
                totalItem.SubItems.Add(totalAmountInPivot.GetFormatedValue(usedCents));
                totalItem.SubItems.Add(totalOlbInPivot.GetFormatedValue(usedCents));
                totalItem.SubItems.Add(ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code);
            }
            else
            {
                totalItem.SubItems.Add(totalAmount.GetFormatedValue(usedCents));
                totalItem.SubItems.Add(totalOlb.GetFormatedValue(usedCents));
                totalItem.SubItems.Add(currencyCodeHolder);
            }

            lvContracts.Items.Add(totalItem);
        }

        public void DisplayUserControl_ViewProject(object sender, EventArgs e)
        {
            _project = (Project)sender;
            AddProject(_project);
        }

        private void personUserControl_ButtonAddProjectClick(object sender, EventArgs e)
        {
            AddProject(null);
        }

        private void corporateUserControl_ButtonAddProjectClick(object sender, EventArgs e)
        {
            AddProject(null);
        }

        private void personUserControl_ButtonBadLoanClick(object sender, EventArgs e)
        {
            _person = _personUserControl.Person;
            InitializeTitle(string.Format("{0} {1}", _person.FirstName, _person.LastName));
        }

        private void personUserControl_ButtonCancelClick(object sender, EventArgs e)
        {
            _person = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void FillDropDownMenuWithPackages(ToolStrip pContextMenu)
        {
            pContextMenu.Items.Clear();
            ProductServices products = ServicesProvider.GetInstance().GetProductServices();
            List<LoanProduct> packages = products.FindAllPackages(false, _oClientType);
            foreach (var package in packages)
            {
                var item = new ToolStripMenuItem(package.Name) { Tag = package.Id };
                item.Click += item_Click;
                pContextMenu.Items.Add(item);
            }
        }

        private void item_Click(object sender, EventArgs e)
        {
            tabControlPerson.TabPages.Clear();
            tclLoanDetails.TabPages.Clear();
            _guarantee = null;

            tabControlPerson.TabPages.Add(tabPageDetails);
            AddProject(_project);
            tabControlPerson.TabPages.Add(tabPageLoansDetails);
            tabControlPerson.TabPages.Add(tabPageAdvancedSettings);
            tabControlPerson.TabPages.Add(tabPageLoanGuarantees);
            tabControlPerson.SelectedTab = tabPageLoansDetails;

            tclLoanDetails.TabPages.Add(tabPageInstallments);
            tclLoanDetails.TabPages.Add(tabPageLoanCustomizableFields);

            int id = (int)((ToolStripMenuItem)sender).Tag;
            _loanShares.Clear();
            _product = ServicesProvider.GetInstance().GetProductServices().FindPackage(id);
            if (_product.CycleId != null)
                ServicesProvider.GetInstance().GetProductServices().SetCyclesParamsForContract(_product, null, _client, true);
            InitializeTabPageLoansDetails(_product);
            _listGuarantors.Clear();
            _collaterals.Clear();
            listViewCollaterals.Items.Clear();

            SetGuarantorsEnabled(_product.UseGuarantorCollateral);
            LoadLoanDetailsExtensions();
            LoadLoanDetailsButtons();
        }

        private void ViewContract()
        {
            try
            {
                if (lvContracts.SelectedItems.Count == 0) return;

                if (lvContracts.SelectedItems[0].Tag is Loan)
                {
                    Loan loan = lvContracts.SelectedItems[0].Tag as Loan;

                    if (loan != null)
                    {
                        if (!loan.IsViewableBy(User.CurrentUser))
                        {
                            Fail("cannotView");
                            return;
                        }
                    }
                    if (loan.Product.CycleId != null)
                    {
                        ServicesProvider.GetInstance().GetProductServices().SetCyclesParamsForContract(loan.Product,
                                                                                                       loan, _client,
                                                                                                       false);
                    }

                    tabControlPerson.TabPages.Remove(tabPageLoansDetails);
                    tabControlPerson.TabPages.Remove(tabPageAdvancedSettings);
                    tabControlPerson.TabPages.Remove(tabPageLoanRepayment);
                    tabControlPerson.TabPages.Remove(tabPageLoanGuarantees);
                    tabControlPerson.TabPages.Remove(tabPageCreditCommitee);
                    tabControlPerson.TabPages.Remove(tabPageSavingDetails);

                    if (lvContracts.FocusedItem.Text == @"C")
                    {
                        _guarantee = null;
                        _credit = (Loan)lvContracts.SelectedItems[0].Tag;
                        _loanShares = _credit.LoanShares;

                        InitializeTabPageLoansDetails(_credit);
                        InitializeTabPageAdvancedSettings();

                        tabControlPerson.TabPages.Add(tabPageLoansDetails);
                        tabControlPerson.TabPages.Add(tabPageAdvancedSettings);
                        tabControlPerson.TabPages.Add(tabPageCreditCommitee);
                        tabControlPerson.TabPages.Add(tabPageLoanGuarantees);

                        if (_credit.Disbursed)
                        {
                            InitializeTabPageLoanRepayment(_credit);
                            tabControlPerson.TabPages.Add(tabPageLoanRepayment);
                            tabControlPerson.SelectedTab = tabPageLoanRepayment;
                        }
                        else
                        {
                            tabControlPerson.SelectedTab = tabPageCreditCommitee;
                        }
                        LoadLoanDetailsExtensions();
                        LoadLoanDetailsButtons();
                    }
                    if (lvContracts.FocusedItem.Text == @"G")
                    {
                        _credit = null;
                        _guarantee = (Guarantee)lvContracts.SelectedItems[0].Tag;
                        tabControlPerson.TabPages.Add(tabPageCreditCommitee);
                        tabControlPerson.SelectedTab = tabPageCreditCommitee;
                    }

                    InitializeCustomizableFields(OCustomizableFieldEntities.Loan, loan.Id,
                                                 loan.ContractStatus == OContractStatus.Active);
                    if (null == _product) return;
                    SetGuarantorsEnabled(_product.UseGuarantorCollateral);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void SetAddTrancheButton(Loan pCredit)
        {
            bool enableButton = pCredit.Product.ActivatedLOC;

            if (pCredit.ClientType == OClientTypes.Group)
                enableButton = false;

            buttonAddTranche.Enabled = enableButton;
        }

        private void InitializeTabPageLoanRepayment(Loan pCredit)
        {
            DisplayListViewLoanRepayments(pCredit);
            DisplayLoanEvents(pCredit);
            buttonLoanReschedule.Enabled = !pCredit.Closed;
            buttonManualSchedule.Enabled = !pCredit.Closed;
            try
            {
                ServiceProvider.GetContractServices().ManualScheduleAfterDisbursement();
            }
            catch (Exception)
            {
                buttonManualSchedule.Enabled = false;
            }
            SetAddTrancheButton(pCredit);
            buttonLoanRepaymentRepay.Enabled = !pCredit.Closed;
            btnWriteOff.Enabled = !pCredit.Closed && !pCredit.WrittenOff;
        }

        private void InitLoanDetails(bool isNew, bool disbursed, bool validated)
        {
            btnSaveLoan.Text = isNew ? GetString("save") : GetString("update");
            //InitialDoclistLoan();

            btnSaveLoan.Enabled = !validated;
            btnUpdateSettings.Enabled = isNew || _credit.PendingOrPostponed();
            buttonLoanPreview.Enabled = (isNew || !validated) && !disbursed;
            buttonLoanDisbursment.Enabled = !disbursed && validated && !isNew;
            if (isNew)
            {
                EconomicActivity newActivity = null;
                switch (_client.Type)
                {
                    case OClientTypes.Person:
                        newActivity = ((Person)_client).Activity;
                        break;
                    case OClientTypes.Group:
                        var group = (Group)_client;
                        var leader = @group.Leader;
                        newActivity = leader == null ? null : ((Person)leader.Tiers).Activity;
                        break;
                    case OClientTypes.Corporate:
                        newActivity = ((Corporate)_client).Activity;
                        break;
                }
                eacLoan.Activity = newActivity;
            }
            else
            {
                eacLoan.Activity = _credit.EconomicActivity;
            }
            eacLoan.Enabled = isNew || _credit.PendingOrPostponed();
            SetSecurityForTabPageLoansDetails(isNew);
            InitLoanEventsPrintButton();
            InitLoanDetailsPrintButton();
            InitLoanRepaymentPrintButton();
            InitCreditCommitteePrintButton();
            InitGuarantorsPrintButton();
        }

        private void InitializeTabPageLoansDetails(Loan pCredit)
        {
            InitializeContractStatus(pCredit);

            gbxLoanDetails.Text = string.Format("{0}{1}  {2}{3}",
                    MultiLanguageStrings.GetString(Ressource.CreditContractForm, "contractCode.Text"), pCredit.Code,
                    MultiLanguageStrings.GetString(Ressource.CreditContractForm, "LoanType.Text"), pCredit.Product.Name);

            InitLoanDetails(false, pCredit.Disbursed, pCredit.ContractStatus == OContractStatus.Validated);
            InitializeTabPageGuaranteesDetailsButtons(pCredit.Product.UseGuarantorCollateral);
            InitializeFundingLine();
            InitializeInstallmentTypes();
            InitializeLoanOfficer();
            SetPackageValuesForLoanDetails(pCredit, false);
            DisableContractDetails(pCredit.ContractStatus);

            DisableCommitteeDecision(pCredit.ContractStatus);

            nudInterestRate.Text = (pCredit.InterestRate * 100).ToString();

            if (pCredit.Product.InterestRate.HasValue)
            {
                nudInterestRate.Enabled = false;
            }

            numericUpDownLoanGracePeriod.Value = (pCredit.GracePeriod.Value);

            btnEditSchedule.Visible = pCredit.Product.AllowFlexibleSchedule;
            btnEditSchedule.Enabled = (pCredit.PendingOrPostponed() && !pCredit.Disbursed);

            textBoxLoanAnticipatedTotalFees.Text = (pCredit.AnticipatedTotalRepaymentPenalties * 100).ToString();
            tbLoanAnticipatedPartialFees.Text = (pCredit.AnticipatedPartialRepaymentPenalties * 100).ToString();

            if (pCredit.Product.AnticipatedTotalRepaymentPenaltiesBase == OAnticipatedRepaymentPenaltiesBases.RemainingOLB)
            {
                lblEarlyTotalRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseOLB.Text");
            }
            else
            {
                lblEarlyTotalRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseInterest.Text");
            }

            if (pCredit.Product.AnticipatedPartialRepaymentPenaltiesBase == OAnticipatedRepaymentPenaltiesBases.RemainingOLB)
            {
                lblEarlyPartialRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseOLB.Text");
            }
            else if (pCredit.Product.AnticipatedPartialRepaymentPenaltiesBase == OAnticipatedRepaymentPenaltiesBases.RemainingInterest)
            {
                lblEarlyPartialRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseInterest.Text");
            }
            else if (pCredit.Product.AnticipatedPartialRepaymentPenaltiesBase == OAnticipatedRepaymentPenaltiesBases.PrepaidPrincipal)
            {
                lblEarlyPartialRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBasePrincipal.Text");
            }

            if (pCredit.AmountUnderLoc.HasValue)
                tbLocAmount.Text = ServicesHelper.ConvertDecimalToString(pCredit.AmountUnderLoc.Value);

            textBoxLoanLateFeesOnAmount.Text = (pCredit.NonRepaymentPenalties.InitialAmount * 100).ToString();
            textBoxLoanLateFeesOnOLB.Text = (pCredit.NonRepaymentPenalties.OLB * 100).ToString();
            textBoxLoanLateFeesOnOverdueInterest.Text = (pCredit.NonRepaymentPenalties.OverDueInterest * 100).ToString();
            textBoxLoanLateFeesOnOverduePrincipal.Text = (pCredit.NonRepaymentPenalties.OverDuePrincipal * 100).ToString();

            groupBoxLoanLateFees.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "LateFeesGracePeriod.Text") + @" (" + pCredit.GracePeriodOfLateFees + @")";

            comboBoxLoanInstallmentType.Text = pCredit.InstallmentType.Name;

            nudLoanNbOfInstallments.Value = pCredit.NbOfInstallments;
            dateLoanStart.Value = pCredit.StartDate;
            _oldDisbursmentDate = dateLoanStart.Value;
            _oldFirstInstalmentDate = dtpDateOfFirstInstallment.Value;
            cmbLoanOfficer.Text = pCredit.LoanOfficer != null ? pCredit.LoanOfficer.Name : string.Empty;

            textBoxLoanPurpose.Text = pCredit.LoanPurpose ?? string.Empty;
            textBoxComments.Text = pCredit.Comments ?? string.Empty;

            SetSecurityForTabPageLoansDetails(false);

            DisplayInstallments(ref pCredit);

            _listGuarantors = _credit.Guarantors;
            _collaterals = _credit.Collaterals;
            lblCreditCurrency.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "Currency.Text") + _credit.Product.Currency.Name;
            DisplayCollateral();
            DisplayGuarantors(pCredit.Guarantors, pCredit.Amount);
            textBoxCreditCommiteeComment.Text = pCredit.CreditCommiteeComment;
            tBCreditCommitteeCode.Text = pCredit.CreditCommitteeCode;

            if (pCredit.CreditCommiteeDate.HasValue)
                dateTimePickerCreditCommitee.Text = pCredit.CreditCommiteeDate.Value.ToShortDateString();
            SetCreditStatus(pCredit.ContractStatus);
            if (pCredit.Code != null)
                textBoxLoanContractCode.Text = pCredit.Code;

            Text = string.Format("{0} - {1}", _title, pCredit.Code);
            InitLoanDetails(false, pCredit.Disbursed,
                            (pCredit.ContractStatus == OContractStatus.Validated) ||
                            (pCredit.ContractStatus == OContractStatus.Active));

        }

        private void SetCreditStatus(OContractStatus pStatus)
        {
            var items = cmbContractStatus.Items.Cast<KeyValuePair<OContractStatus, string>>();
            KeyValuePair<OContractStatus, string> itemToSelect;
            switch (pStatus)
            {
                case OContractStatus.Pending:
                case OContractStatus.Postponed:
                case OContractStatus.Refused:
                case OContractStatus.Abandoned:
                    itemToSelect = items.First(item => item.Key == pStatus);
                    break;
                case OContractStatus.Validated:
                case OContractStatus.Active:
                case OContractStatus.Closed:
                case OContractStatus.WrittenOff:
                    itemToSelect = items.First(item => item.Key == OContractStatus.Validated);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("No such status: {0}", pStatus.GetName()));
            }
            cmbContractStatus.SelectedItem = itemToSelect;
        }

        private void DisableContractDetails(OContractStatus pContractStatus)
        {
            bool isPendingOrPostponed = pContractStatus == OContractStatus.Pending || pContractStatus == OContractStatus.Postponed;

            nudLoanAmount.Enabled = isPendingOrPostponed;
            nudInterestRate.Enabled = isPendingOrPostponed;
            numericUpDownLoanGracePeriod.Enabled = isPendingOrPostponed;
            nudLoanNbOfInstallments.Enabled = isPendingOrPostponed;
            eacLoan.Enabled = isPendingOrPostponed;
            textBoxLoanAnticipatedTotalFees.Enabled = isPendingOrPostponed;
            tbLoanAnticipatedPartialFees.Enabled = isPendingOrPostponed;
            comboBoxLoanInstallmentType.Enabled = isPendingOrPostponed;
            groupBoxLoanLateFees.Enabled = isPendingOrPostponed;
            dateLoanStart.Enabled = dtpDateOfFirstInstallment.Enabled = isPendingOrPostponed;

            //comboBoxLoanCorporate.Enabled = isPending;
            comboBoxLoanFundingLine.Enabled = isPendingOrPostponed;
            cmbLoanOfficer.Enabled = isPendingOrPostponed;
            textBoxLoanPurpose.ReadOnly = !isPendingOrPostponed;
            textBoxComments.Enabled = isPendingOrPostponed;
            comboBoxLoanFundingLine.Enabled = isPendingOrPostponed;
            groupBoxEntryFees.Enabled = isPendingOrPostponed;
            EnableInsuranceTextBox(_credit);
            EnableLocAmountTextBox(_credit);
        }

        private void SetCcEnabled(bool enabled)
        {
            pnlCCStatus.Enabled = enabled;
            dateTimePickerCreditCommitee.Enabled = enabled;
            tBCreditCommitteeCode.Enabled = enabled;
            textBoxCreditCommiteeComment.Enabled = enabled;
            buttonCreditCommiteeSaveDecision.Enabled = enabled;
            btnPrintCreditCommittee.Enabled = true;
        }

        private void DisableCommitteeDecision(OContractStatus pContractStatus)
        {
            if (pContractStatus != OContractStatus.Closed && !_credit.Disbursed)
            {
                SetCcEnabled(true);
                if (pContractStatus != OContractStatus.Pending)
                {
                    EnableBoxCreditCommitteeComponents(false);
                    buttonCreditCommiteeSaveDecision.Enabled = true;
                    buttonCreditCommiteeSaveDecision.Text = GetString("update");
                }
            }
            else
            {
                buttonCreditCommiteeSaveDecision.Text = GetString("save");
                SetCcEnabled(false);
            }

        }

        private void EnableBoxCreditCommitteeComponents(bool toEnable)
        {
            pnlCCStatus.Enabled = toEnable;
            dateTimePickerCreditCommitee.Enabled = toEnable;
            tBCreditCommitteeCode.Enabled = toEnable;
            textBoxCreditCommiteeComment.Enabled = toEnable;
        }

        private void InitializeTabPageAdvancedSettings()
        {
            InitializeEntryFees();
            SetCreditInsurance();
            if (_credit != null)
            {
                cmbCompulsorySaving.Enabled = _credit.Product.UseCompulsorySavings && !_credit.Disbursed;
                linkCompulsorySavings.Enabled = _credit.Product.UseCompulsorySavings;
                numCompulsoryAmountPercent.Enabled = _credit.Product.UseCompulsorySavings && !_credit.Disbursed;
            }
        }

        private void SetCreditInsurance()
        {
            lblInsuranceMin.Text = _credit.Product.CreditInsuranceMin.ToString("0.00");
            lblInsuranceMax.Text = _credit.Product.CreditInsuranceMax.ToString("0.00");
            if (_credit.Id == 0)
            {
                tbInsurance.Text = _credit.Product.CreditInsuranceMin.ToString("0.00");
            }
            else
            {
                tbInsurance.Text = _credit.Insurance.ToString("0.00");
            }
        }

        private void InitializeEntryFees()
        {
            lvEntryFees.Items.Clear();

            if (_credit.Id == 0)
                _credit.LoanEntryFeesList = ServicesProvider.GetInstance().GetContractServices().GetDefaultLoanEntryFees(_credit, _client);
            else
                _credit.LoanEntryFeesList =
                    ServicesProvider.GetInstance().GetContractServices().GetInstalledLoanEntryFees(_credit);

            foreach (LoanEntryFee entryFee in _credit.LoanEntryFeesList)
            {
                ListViewItem item = new ListViewItem(entryFee.ProductEntryFee.Name)
                                        {
                                            UseItemStyleForSubItems = true,
                                            Tag = entryFee
                                        };

                OCurrency feeValue = entryFee.FeeValue;
                if (entryFee.ProductEntryFee.IsRate)
                    item.SubItems.Add(feeValue.GetFormatedValue(true));
                else
                    item.SubItems.Add(feeValue.GetFormatedValue(_credit.Product.Currency.UseCents));

                _typeOfFee = entryFee.ProductEntryFee.IsRate ? "%" : _credit.Product.Currency.Name;

                item.SubItems.Add(_typeOfFee);
                OCurrency loanAmount = nudLoanAmount.Value;
                OCurrency amount;
                if (entryFee.ProductEntryFee.IsRate)
                    amount = loanAmount.Value * feeValue.Value / 100;
                else
                    amount = feeValue.Value;
                item.SubItems.Add(amount.GetFormatedValue(_credit.Product.Currency.UseCents));

                lvEntryFees.Items.Add(item);
            }

            lvEntryFees.Columns[2].Text = string.Format("% / {0}", _credit.Product.Currency.Name);
            string total = MultiLanguageStrings.GetString(Ressource.ClientForm, "TotalEntryFees");
            ListViewItem itemTotal = new ListViewItem("")
                                         {
                                             UseItemStyleForSubItems = true,
                                             Tag = "TotalFees"
                                         };
            itemTotal.Font = new Font("Arial", 9F, FontStyle.Bold);

            itemTotal.SubItems.Add("");
            itemTotal.SubItems.Add(total);
            itemTotal.SubItems.Add("");
            itemTotal.SubItems.Add("");
            ShowTotalFeesInListView(itemTotal);
            lvEntryFees.Items.Add(itemTotal);
        }

        /// <summary>
        /// The method initializes values of controls of the Details tab when a contract is creating
        /// </summary>
        /// <param name="pPackage">Loan product</param>
        private void InitializeTabPageLoansDetails(LoanProduct pPackage)
        {
            _credit = new Loan(User.CurrentUser, ServicesProvider.GetInstance().GetGeneralSettings(),
                ServicesProvider.GetInstance().GetNonWorkingDate(),
                CoreDomainProvider.GetInstance().GetProvisioningTable(),
                CoreDomainProvider.GetInstance().GetChartOfAccounts());
            _copyOfCredit = _credit;
            _credit.Product = pPackage;

            nudLoanAmount.Text = string.Empty;
            textBoxLoanContractCode.Text = string.Empty;
            ReInitializeListViewInstallment();
            pnlCCStatus.Enabled = true;

            InitializeFundingLine();
            InitializeInstallmentTypes();
            InitializeLoanOfficer();
            SetPackageValuesForLoanDetails(_credit, true);
            SetSecurityForTabPageLoansDetails(true);
            InitLoanDetails(true, false, false);
            InitializeTabPageGuaranteesDetailsButtons(_credit.Product.UseGuarantorCollateral);
            cmbLoanOfficer.Enabled = true;
            textBoxLoanPurpose.Enabled = true;
            textBoxComments.Enabled = true;
            listViewLoanInstallments.Items.Clear();
            listViewGuarantors.Items.Clear();
            lblCreditCurrency.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "Currency.Text") + pPackage.Currency.Name;
            lbCompulsorySavings.Enabled = false;
            InitializePackageLoanCompulsorySavings(pPackage, true);

            btnEditSchedule.Visible = _credit.Product.AllowFlexibleSchedule;
            btnEditSchedule.Enabled = ((_credit.ContractStatus == 0 || _credit.PendingOrPostponed()) && !_credit.Disbursed);
            try
            {
                ServiceProvider.GetContractServices().ManualScheduleBeforeDisbursement();
            }
            catch (Exception)
            {
                btnEditSchedule.Enabled = false;
            }
            EnableLocAmountTextBox(_credit);
            EnableInsuranceTextBox(_credit);
            InitializeTabPageAdvancedSettings();

            bool active = _credit != null && _credit.ContractStatus == OContractStatus.Active;
            numCompulsoryAmountPercent.BackColor = pPackage.UseCompulsorySavings ? Color.White : Color.LightGray;
            InitializeCustomizableFields(OCustomizableFieldEntities.Loan, _credit.Id, active);

        }

        private void EnableLocAmountTextBox(Loan credit)
        {
            if ((credit.PendingOrPostponed() || credit.ContractStatus == 0) &&
                (credit.Product.AmountUnderLocMin.HasValue && credit.Product.AmountUnderLocMax.HasValue))
                tbLocAmount.Enabled = true;
            else
                tbLocAmount.Enabled = false;

            if (credit.Product.AmountUnderLocMin.HasValue && credit.Product.AmountUnderLocMax.HasValue)
            {
                labelLocMaxAmount.Text = ServicesHelper.ConvertDecimalToString(credit.Product.AmountUnderLocMax.Value);
                labelLocMinAmount.Text = ServicesHelper.ConvertDecimalToString(credit.Product.AmountUnderLocMin.Value);
                if (!credit.AmountUnderLoc.HasValue)
                    tbLocAmount.Text = credit.Product.AmountUnderLocMax.GetFormatedValue(credit.UseCents);
                lblLocCurrencyMin.Text = _credit.Product.Currency.Name;
                lblLocCurrencyMax.Text = _credit.Product.Currency.Name;
                lblLocCurrencyMin.Visible = true;
                lblLocCurrencyMax.Visible = true;
                labelLocMinAmount.Visible = true;
                labelLocMaxAmount.Visible = true;
                labelLocMin.Visible = true;
                labelLocMax.Visible = true;
            }
            else if (credit.Product.AmountUnderLoc.HasValue)
            {
                tbLocAmount.Text = credit.Product.AmountUnderLoc.GetFormatedValue(credit.UseCents);
                labelLocMaxAmount.Text = ServicesHelper.ConvertDecimalToString(credit.Product.AmountUnderLoc.Value);
                labelLocMinAmount.Text = ServicesHelper.ConvertDecimalToString(credit.Product.AmountUnderLoc.Value);
                lblLocCurrencyMin.Text = _credit.Product.Currency.Name;
                lblLocCurrencyMax.Text = _credit.Product.Currency.Name;
                lblLocCurrencyMin.Visible = true;
                lblLocCurrencyMax.Visible = true;
                labelLocMinAmount.Visible = true;
                labelLocMaxAmount.Visible = true;
                labelLocMin.Visible = true;
                labelLocMax.Visible = true;

            }
            else
            {
                tbLocAmount.Enabled = false;
                tbLocAmount.Text = "0";
            }

        }

        private void SetLoanOfficer(User pUser)
        {
            if (_person != null && _person.FavouriteLoanOfficer != null) return;
            if (_group != null && _group.FavouriteLoanOfficer != null) return;
            if (_corporate != null && _corporate.FavouriteLoanOfficer != null) return;

            foreach (object item in cmbLoanOfficer.Items)
            {
                if (item is User)
                {
                    User user = (User)item;
                    if (user.ToString() == pUser.ToString())
                    {
                        cmbLoanOfficer.SelectedItem = user;
                    }
                }
            }
        }

        private void SetSecurityForTabPageLoansDetails(bool pNewContract)
        {
            if (pNewContract) SetLoanOfficer(User.CurrentUser);

            if (_credit.Closed || _credit.ContractStatus.Equals(OContractStatus.Abandoned)
                || _credit.ContractStatus.Equals(OContractStatus.Refused))
            {
                btnSaveLoan.Enabled = false;
            }
        }

        private void InitializeLoanOfficer()
        {
            cmbLoanOfficer.Items.Clear();
            _subordinates = new List<User>();
            _subordinates.Add(User.CurrentUser);
            _subordinates.AddRange(User.CurrentUser.Subordinates);
            _subordinates.Sort();

            foreach (User user in _subordinates)
            {
                if (!user.IsDeleted && user.UserRole.IsRoleForLoan)
                    cmbLoanOfficer.Items.Add(user);
            }
            // set favoutite loan officer
            if (_credit.LoanOfficer != null)
            {
                cmbLoanOfficer.Text = _credit.LoanOfficer.Name;
            }
            else
            {
                if (_person != null)
                {
                    _person = ServicesProvider.GetInstance().GetClientServices().FindPersonById(_person.Id);
                    if (_person.FavouriteLoanOfficer != null)
                        if (!_person.FavouriteLoanOfficer.IsDeleted)
                            cmbLoanOfficer.Text = _person.FavouriteLoanOfficer.Name;
                        else
                            cmbLoanOfficer.SelectedIndex = 0;
                }
                else if (_group != null)
                {
                    if (_group.FavouriteLoanOfficer != null)
                        if (!_group.FavouriteLoanOfficer.IsDeleted)
                            cmbLoanOfficer.Text = _group.FavouriteLoanOfficer.Name;
                        else cmbLoanOfficer.SelectedIndex = 0;

                }
                else if (_corporate != null)
                {
                    if (_corporate.FavouriteLoanOfficer != null)
                        if (!_corporate.FavouriteLoanOfficer.IsDeleted)
                            cmbLoanOfficer.Text = _corporate.FavouriteLoanOfficer.Name;
                }
                else
                    cmbLoanOfficer.SelectedIndex = 0;
            }
        }

        private void InitializeFundingLine()
        {

            comboBoxLoanFundingLine.Items.Clear();
            List<FundingLine> fundingLines = ServicesProvider.GetInstance().GetFundingLinesServices().SelectFundingLines();

            foreach (FundingLine line in fundingLines)
                comboBoxLoanFundingLine.Items.Add(line);

            comboBoxLoanFundingLine.Items.Insert(0, MultiLanguageStrings.GetString(Ressource.CreditContractForm, "select.Text"));
            comboBoxLoanFundingLine.SelectedIndex = 0;
        }

        private void InitializeGuaranteeCorporate()
        {
            List<FundingLine> fundingLines = ServicesProvider.GetInstance().GetFundingLinesServices().SelectFundingLines();
        }

        private void InitializeInstallmentTypes()
        {
            comboBoxLoanInstallmentType.Items.Clear();
            List<InstallmentType> installmentTypeList = ServicesProvider.GetInstance().GetProductServices().FindAllInstallmentTypes();
            comboBoxLoanInstallmentType.Items.Insert(0, MultiLanguageStrings.GetString(Ressource.CreditContractForm, "selectInstallmentType.Text"));

            foreach (InstallmentType installmentType in installmentTypeList)
                comboBoxLoanInstallmentType.Items.Add(installmentType);

            comboBoxLoanInstallmentType.SelectedItem = 0;
        }

        private void InitializeLabelMinMax()
        {
            labelLoanAmountMinMax.Text = String.Empty;
            lbLoanInterestRateMinMax.Text = String.Empty;
            labelLoanAnticipatedTotalFeesMinMax.Text = String.Empty;
            lblLoanAnticipatedPartialFeesMinMax.Text = String.Empty;
            labelLoanLateFeesOnOLBMinMax.Text = String.Empty;
            labelLoanLateFeesOnAmountMinMax.Text = String.Empty;
            labelLoanLateFeesOnOverdueInterestMinMax.Text = String.Empty;
            labelLoanLateFeesOnOverduePrincipalMinMax.Text = String.Empty;

            labelLoanNbOfInstallmentsMinMax.Text = String.Empty;
            labelLoanGracePeriodMinMax.Text = String.Empty;
        }

        private void SetPackageValuesForLoanDetails(Loan pLoan, bool pForCreation)
        {
            gbxLoanDetails.Text = MultiLanguageStrings.GetString(Ressource.CreditContractForm, "LoanType.Text") + pLoan.Product.Name;
            if (pForCreation)
            {
                dateLoanStart.Value = TimeProvider.Today;
            }
            else
            {
                _toChangeAlignDate = false;
                dateLoanStart.Value = _credit.StartDate;
                _toChangeAlignDate = true;
            }
            _oldDisbursmentDate = dateLoanStart.Value;
            dtpDateOfFirstInstallment.Value = pForCreation ? GetFirstInstallmentDate() : _credit.FirstInstallmentDate;
            _oldFirstInstalmentDate = dtpDateOfFirstInstallment.Value;
            InitializeLabelMinMax();
            InitializePackageGracePeriod(pLoan.Product, pForCreation);
            InitializeAmount(pLoan, pForCreation);
            InitializePackageInterestRate(pLoan, pForCreation);
            InitializePackageFundingLineAndCorporate(pLoan.Product.FundingLine, _credit.FundingLine, pForCreation, comboBoxLoanFundingLine);
            InitializePackageInstallmentType(pLoan.Product, pForCreation);
            InitializePackageNumberOfInstallments(pLoan, pForCreation);
            InitializePackageAnticipatedTotalRepaymentsPenalties(pLoan.Product, pForCreation);
            InitializePackageAnticipatedPartialRepaymentsPenalties(pLoan.Product, pForCreation);
            InitializePackageNonRepaymentPenalties(pLoan.Product, pForCreation);
            InitializePackageLoanCompulsorySavings(pLoan.Product, pForCreation);
            _changeDisDateBool = false;
        }

        private void InitializePackageFundingLineAndCorporate(Object packageObj, Object creditObj, bool pForCreation,
                                                               ComboBox cmbFundingCorporateDetails)
        {
            cmbFundingCorporateDetails.Enabled = true;
            cmbFundingCorporateDetails.ForeColor = Color.Black;
            cmbFundingCorporateDetails.Font = new Font(Font, FontStyle.Regular);

            if (pForCreation)
            {
                if (packageObj != null)
                {
                    cmbFundingCorporateDetails.Text = packageObj.ToString();
                    cmbFundingCorporateDetails.Tag = packageObj;
                    return;
                }
                cmbFundingCorporateDetails.Enabled = true;
                cmbFundingCorporateDetails.Text = "";
                cmbFundingCorporateDetails.Tag = null;
                return;
            }
            if (creditObj != null)
            {
                cmbFundingCorporateDetails.Text = creditObj.ToString();
                cmbFundingCorporateDetails.Tag = creditObj;
                return;
            }
            cmbFundingCorporateDetails.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractIsReadOnly.Text");
            cmbFundingCorporateDetails.ForeColor = System.Drawing.Color.Red;
            cmbFundingCorporateDetails.Font = new Font(this.Font, FontStyle.Bold);
            MessageBox.Show(MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractIsReadOnly.Text"));
            cmbFundingCorporateDetails.Tag = null;
        }

        private DoubleValueRange _lateFeesOnAmountRangeValue;
        private DoubleValueRange _lateFeesOnOLBRangeValue;
        private DoubleValueRange _lateFeesOnOverdueInterestRangeValue;
        private DoubleValueRange _lateFeesOnOverduePrincipalRangeValue;
        private void InitializePackageNonRepaymentPenalties(LoanProduct pPackage, bool pForCreation)
        {
            groupBoxLoanLateFees.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "LateFeesGracePeriod.Text") + " (" + pPackage.GracePeriodOfLateFees + ")";
            _gracePeriodOfLateFees = pPackage.GracePeriodOfLateFees;

            #region InitialAmount
            if (!pPackage.NonRepaymentPenalties.InitialAmount.HasValue) //Min and Max
            {
                _lateFeesOnAmountRangeValue = new DoubleValueRange(pPackage.NonRepaymentPenaltiesMin.InitialAmount, pPackage.NonRepaymentPenaltiesMax.InitialAmount);
                textBoxLoanLateFeesOnAmount.Enabled = true;
                labelLoanLateFeesOnAmountMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnAmountRangeValue.Min, true),
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnAmountRangeValue.Max, true));
                textBoxLoanLateFeesOnAmount.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnAmountRangeValue.Min, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.InitialAmount, true);
            }
            else
                textBoxLoanLateFeesOnAmount.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(pPackage.NonRepaymentPenalties.InitialAmount, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.InitialAmount, true);
            #endregion

            #region OLB
            if (!pPackage.NonRepaymentPenalties.OLB.HasValue) //Min and Max
            {
                _lateFeesOnOLBRangeValue = new DoubleValueRange(pPackage.NonRepaymentPenaltiesMin.OLB, pPackage.NonRepaymentPenaltiesMax.OLB);
                textBoxLoanLateFeesOnOLB.Enabled = true;
                labelLoanLateFeesOnOLBMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOLBRangeValue.Min, true),
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOLBRangeValue.Max, true));
                textBoxLoanLateFeesOnOLB.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOLBRangeValue.Min, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.OLB, true);
            }
            else
                if (pForCreation)
                {
                    textBoxLoanLateFeesOnOLB.Text = ServicesHelper.ConvertNullableDoubleToString(pPackage.NonRepaymentPenalties.OLB, true);
                }
                else
                {
                    textBoxLoanLateFeesOnOLB.Text = ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.OLB, true);
                }
            #endregion

            #region OverdueINterest
            if (!pPackage.NonRepaymentPenalties.OverDueInterest.HasValue) //Min and Max
            {
                _lateFeesOnOverdueInterestRangeValue = new DoubleValueRange(pPackage.NonRepaymentPenaltiesMin.OverDueInterest, pPackage.NonRepaymentPenaltiesMax.OverDueInterest);
                textBoxLoanLateFeesOnOverdueInterest.Enabled = true;
                labelLoanLateFeesOnOverdueInterestMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOverdueInterestRangeValue.Min, true),
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOverdueInterestRangeValue.Max, true));
                if (pForCreation)
                {
                    textBoxLoanLateFeesOnOverdueInterest.Text = ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOverdueInterestRangeValue.Min, true);
                }
                else
                {
                    textBoxLoanLateFeesOnOverdueInterest.Text = ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.OverDueInterest, true);
                }
            }
            else
                if (pForCreation)
                {
                    textBoxLoanLateFeesOnOverdueInterest.Text = ServicesHelper.ConvertNullableDoubleToString(pPackage.NonRepaymentPenalties.OverDueInterest, true);
                }
                else
                {
                    textBoxLoanLateFeesOnOverdueInterest.Text = ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.OverDueInterest, true);
                }
            #endregion

            #region OverduePrincipal
            if (!pPackage.NonRepaymentPenalties.OverDuePrincipal.HasValue) //Min and Max
            {
                _lateFeesOnOverduePrincipalRangeValue = new DoubleValueRange(pPackage.NonRepaymentPenaltiesMin.OverDuePrincipal, pPackage.NonRepaymentPenaltiesMax.OverDuePrincipal);
                textBoxLoanLateFeesOnOverduePrincipal.Enabled = true;
                labelLoanLateFeesOnOverduePrincipalMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOverduePrincipalRangeValue.Min, true),
                                  MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                                  ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOverduePrincipalRangeValue.Max, true));
                if (pForCreation)
                {
                    textBoxLoanLateFeesOnOverduePrincipal.Text = ServicesHelper.ConvertNullableDoubleToString(_lateFeesOnOverduePrincipalRangeValue.Min, true);
                }
                else
                {
                    textBoxLoanLateFeesOnOverduePrincipal.Text = ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.OverDuePrincipal, true);
                }
            }
            else
                if (pForCreation)
                {
                    textBoxLoanLateFeesOnOverduePrincipal.Text = ServicesHelper.ConvertNullableDoubleToString(pPackage.NonRepaymentPenalties.OverDuePrincipal, true);
                }
                else
                {
                    textBoxLoanLateFeesOnOverduePrincipal.Text = ServicesHelper.ConvertNullableDoubleToString(_credit.NonRepaymentPenalties.OverDuePrincipal, true);
                }
            #endregion

            if (pPackage.AnticipatedTotalRepaymentPenaltiesBase == OAnticipatedRepaymentPenaltiesBases.RemainingOLB)
            {
                lblEarlyTotalRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseOLB.Text");
            }
            else
            {
                lblEarlyTotalRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseInterest.Text");
            }

            if (pPackage.AnticipatedPartialRepaymentPenaltiesBase == OAnticipatedRepaymentPenaltiesBases.RemainingOLB)
            {
                lblEarlyPartialRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseOLB.Text");
            }
            else
            {
                lblEarlyPartialRepaimentBase.Text = MultiLanguageStrings.GetString(Ressource.ClientForm, "lblEarlyRepaimentBaseInterest.Text");
            }
        }

        private void SetCompulsorySavingRange(int min, int max, int value)
        {
            numCompulsoryAmountPercent.Minimum = min;
            numCompulsoryAmountPercent.Maximum = max;
            numCompulsoryAmountPercent.Value = value;
        }

        private void InitializePackageLoanCompulsorySavings(LoanProduct pPackage, bool pForCreation)
        {
            if (!pForCreation)
            {
                var pendingOrPostponed = _credit.PendingOrPostponed();
                SetSavingControlsState(pendingOrPostponed);
                if (_credit.CompulsorySavings != null) // Loan exists already
                {
                    if (!pendingOrPostponed)
                    {
                        var loanSavingValue = _credit.CompulsorySavingsPercentage ?? 0;
                        SetCompulsorySavingRange(loanSavingValue, loanSavingValue, loanSavingValue);
                        lbCompAmountPercentMinMax.Text = string.Format("{0}%", loanSavingValue);
                        DisableAndLoadSavingControls(); // Loan is not editable                        
                    }
                    else
                    {
                        InitCompulsorySavingRange(pPackage);
                        EnableAndLoadSavingControls();
                    }
                }
            }
            else
            {
                InitCompulsorySavingRange(pPackage);
                EnableAndLoadSavingControls();
            }
        }

        private void InitCompulsorySavingRange(LoanProduct pPackage)
        {
            if (pPackage.UseCompulsorySavings)
            {
                lbCompAmountPercentMinMax.Visible = true;
                if (pPackage.CompulsoryAmount.HasValue)
                {
                    var compulsoryAmount = pPackage.CompulsoryAmount.Value;
                    SetCompulsorySavingRange(compulsoryAmount, compulsoryAmount, compulsoryAmount);
                    lbCompAmountPercentMinMax.Text = string.Format("{0}%", compulsoryAmount);
                }
                else
                {
                    Debug.Assert(pPackage.CompulsoryAmountMin != null,
                                 "Product should have compulsory min value, if it is compulsary and does not have compulsory amount");
                    Debug.Assert(pPackage.CompulsoryAmountMax != null,
                                 "Product should have compulsory max value, if it is compulsary and does not have compulsory amount");
                    var minCompulsoryValue = pPackage.CompulsoryAmountMin.Value;
                    var maxCompulsoryValue = pPackage.CompulsoryAmountMax.Value;
                    var loanSavingValue = _credit.CompulsorySavingsPercentage ?? 0;

                    SetCompulsorySavingRange(
                        minCompulsoryValue,
                        maxCompulsoryValue,
                        Math.Min(
                            Math.Max(loanSavingValue, minCompulsoryValue),
                            maxCompulsoryValue
                            )
                        );

                    lbCompAmountPercentMinMax.Text =
                        string.Format("{0}{1}%\r\n{2}{3}%",
                                      MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                                      minCompulsoryValue,
                                      MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                                      maxCompulsoryValue
                            );
                }
            }
            else
            {
                numCompulsoryAmountPercent.Maximum = int.MaxValue;
                numCompulsoryAmountPercent.Minimum = 0;
                numCompulsoryAmountPercent.Value = 0;
                lbCompAmountPercentMinMax.Visible = false;
            }
        }

        private void InitializePackageNumberOfInstallments(Loan credit, bool pForCreation)
        {
            if (pForCreation)
            {
                nudLoanNbOfInstallments.Enabled = true;
                if (credit.Product.CycleId == null)//if product doesn't use a loan cycle
                {
                    nudLoanNbOfInstallments.Enabled = true;
                    if (credit.Product.NbOfInstallments.HasValue)
                    {
                        nudLoanNbOfInstallments.Minimum = credit.Product.NbOfInstallments.Value;
                        nudLoanNbOfInstallments.Maximum = credit.Product.NbOfInstallments.Value;
                        labelLoanNbOfInstallmentsMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                         MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                         credit.Product.NbOfInstallments.Value,
                         MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                         credit.Product.NbOfInstallments.Value);
                        nudLoanNbOfInstallments.Value = credit.Product.NbOfInstallments.Value;
                    }
                    else
                    {
                        nudLoanNbOfInstallments.Minimum = credit.Product.NbOfInstallmentsMin.Value;
                        nudLoanNbOfInstallments.Maximum = credit.Product.NbOfInstallmentsMax.Value;
                        labelLoanNbOfInstallmentsMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                         MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                         credit.Product.NbOfInstallmentsMin.Value,
                         MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                         credit.Product.NbOfInstallmentsMax.Value);
                        nudLoanNbOfInstallments.Value = credit.Product.NbOfInstallmentsMin.Value;
                    }
                }
                else //product uses a loan cycle
                {
                    nudLoanNbOfInstallments.Minimum = credit.Product.NbOfInstallmentsMin.Value;
                    nudLoanNbOfInstallments.Maximum = credit.Product.NbOfInstallmentsMax.Value;
                    labelLoanNbOfInstallmentsMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                     MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                     credit.Product.NbOfInstallmentsMin.Value,
                     MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                     credit.Product.NbOfInstallmentsMax.Value);
                    nudLoanNbOfInstallments.Value = credit.Product.NbOfInstallmentsMin.Value;
                }
            }
            else//it is an existing contract
            {
                if (credit.Product.CycleId == null)//contract doesn't use loan cycles
                {
                    if (!credit.Product.NbOfInstallments.HasValue)//if it is range value
                    {
                        try
                        {
                            nudLoanNbOfInstallments.Minimum = credit.Product.NbOfInstallmentsMin.Value;
                            nudLoanNbOfInstallments.Maximum = credit.Product.NbOfInstallmentsMax.Value;
                            labelLoanNbOfInstallmentsMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                             MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                             credit.Product.NbOfInstallmentsMin.Value,
                             MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                             credit.Product.NbOfInstallmentsMax.Value);
                            nudLoanNbOfInstallments.Value = credit.NbOfInstallments;
                        }
                        catch
                        {
                            nudLoanNbOfInstallments.Minimum = nudLoanNbOfInstallments.Maximum = credit.NbOfInstallments;
                            nudLoanNbOfInstallments.Value = credit.NbOfInstallments;
                        }
                    }
                    else
                    {
                        try
                        {
                            nudLoanNbOfInstallments.Minimum = credit.Product.NbOfInstallments.Value;
                            nudLoanNbOfInstallments.Maximum = credit.Product.NbOfInstallments.Value;
                            labelLoanNbOfInstallmentsMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                             MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                             credit.Product.NbOfInstallments.Value,
                             MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                             credit.Product.NbOfInstallments.Value);
                            nudLoanNbOfInstallments.Value = credit.NbOfInstallments;
                        }
                        catch
                        {
                            nudLoanNbOfInstallments.Minimum = nudLoanNbOfInstallments.Maximum = credit.NbOfInstallments;
                            nudLoanNbOfInstallments.Value = credit.NbOfInstallments;
                        }
                    }
                }
                else //contract uses loan cycle
                {
                    try
                    {
                        nudLoanNbOfInstallments.Minimum = credit.NmbOfInstallmentsMin.Value;
                        nudLoanNbOfInstallments.Maximum = credit.NmbOfInstallmentsMin.Value;
                        labelLoanNbOfInstallmentsMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                         MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                         credit.Product.NbOfInstallmentsMin.Value,
                         MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                         credit.Product.NbOfInstallmentsMax.Value);
                        nudLoanNbOfInstallments.Value = credit.NbOfInstallments;
                    }
                    catch
                    {
                        nudLoanNbOfInstallments.Minimum = nudLoanNbOfInstallments.Maximum = credit.NbOfInstallments;
                        nudLoanNbOfInstallments.Value = credit.NbOfInstallments;
                    }
                }
            }
        }



        private void InitializePackageAnticipatedTotalRepaymentsPenalties(LoanProduct pPackage, bool pForCreation)
        {
            if (!pPackage.AnticipatedTotalRepaymentPenalties.HasValue) //Min and Max
            {
                _anticipatedTotalFeesValueRange = new DoubleValueRange(pPackage.AnticipatedTotalRepaymentPenaltiesMin, pPackage.AnticipatedTotalRepaymentPenaltiesMax);

                textBoxLoanAnticipatedTotalFees.Enabled = true;
                labelLoanAnticipatedTotalFeesMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                    MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                    ServicesHelper.ConvertNullableDoubleToString(_anticipatedTotalFeesValueRange.Min, true),
                    MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                    ServicesHelper.ConvertNullableDoubleToString(_anticipatedTotalFeesValueRange.Max, true));
                textBoxLoanAnticipatedTotalFees.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(_anticipatedTotalFeesValueRange.Min, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.AnticipatedTotalRepaymentPenalties, true);
            }
            else
            {
                textBoxLoanAnticipatedTotalFees.Enabled = false;
                _anticipatedTotalFeesValueRange = new DoubleValueRange(pPackage.AnticipatedTotalRepaymentPenalties);
                textBoxLoanAnticipatedTotalFees.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(pPackage.AnticipatedTotalRepaymentPenalties, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.AnticipatedTotalRepaymentPenalties, true);
            }
        }

        private void InitializePackageAnticipatedPartialRepaymentsPenalties(LoanProduct pPackage, bool pForCreation)
        {
            if (!pPackage.AnticipatedPartialRepaymentPenalties.HasValue) //Min and Max
            {
                _anticipatedPartialFeesValueRange = new DoubleValueRange(pPackage.AnticipatedPartialRepaymentPenaltiesMin, pPackage.AnticipatedPartialRepaymentPenaltiesMax);

                tbLoanAnticipatedPartialFees.Enabled = true;
                lblLoanAnticipatedPartialFeesMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                    MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                    ServicesHelper.ConvertNullableDoubleToString(_anticipatedPartialFeesValueRange.Min, true),
                    MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                    ServicesHelper.ConvertNullableDoubleToString(_anticipatedPartialFeesValueRange.Max, true));
                tbLoanAnticipatedPartialFees.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(_anticipatedPartialFeesValueRange.Min, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.AnticipatedPartialRepaymentPenalties, true);
            }
            else
            {
                tbLoanAnticipatedPartialFees.Enabled = false;
                _anticipatedPartialFeesValueRange = new DoubleValueRange(pPackage.AnticipatedPartialRepaymentPenalties);
                tbLoanAnticipatedPartialFees.Text = pForCreation ? ServicesHelper.ConvertNullableDoubleToString(pPackage.AnticipatedPartialRepaymentPenalties, true) : ServicesHelper.ConvertNullableDoubleToString(_credit.AnticipatedPartialRepaymentPenalties, true);
            }
        }

        private void InitializePackageInstallmentType(LoanProduct pPackage, bool pForCreation)
        {
            if (pForCreation)
            {
                comboBoxLoanInstallmentType.Text = pPackage.InstallmentType.Name;
                comboBoxLoanInstallmentType.Tag = pPackage.InstallmentType.Id;
            }
            else
            {
                comboBoxLoanInstallmentType.Text = _credit.InstallmentType.Name;
                comboBoxLoanInstallmentType.Tag = _credit.InstallmentType.Id;
            }
        }


        private void InitializePackageInterestRate(Loan credit, bool pForCreation)
        {
            LoanProduct creditProduct = credit.Product;
            string annualType = creditProduct.LoanType == OLoanTypes.DecliningFixedPrincipalWithRealInterest
                                       ? MultiLanguageStrings.GetString(Ressource.CreditContractForm, "annual.Text")
                                       + "\n"
                                       : "";

            if (pForCreation) //if it is new contract
            {
                nudInterestRate.Enabled = true;
                if (!creditProduct.UseLoanCycle) //if product doesn't use any loan cycles
                {
                    if (!creditProduct.InterestRate.HasValue) //if interest rate is a range value
                    {
                        decimal? interestRateMin = creditProduct.InterestRateMin * 100;
                        decimal? interestRateMax = creditProduct.InterestRateMax * 100;
                        nudInterestRate.Minimum = interestRateMin.Value;
                        nudInterestRate.Maximum = interestRateMax.Value;
                        lbLoanInterestRateMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                            annualType + MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRateMin.Value, false),
                            MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRateMax.Value, false));
                        nudInterestRate.Value = interestRateMin.Value;
                    }
                    else// if interest rate is a fixed value
                    {
                        decimal? interestRate = creditProduct.InterestRate * 100;
                        nudInterestRate.Minimum = nudInterestRate.Maximum = creditProduct.InterestRate.Value * 100;
                        nudInterestRate.Value = interestRate.Value;
                        lbLoanInterestRateMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                            annualType + MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRate.Value, false),
                            MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRate.Value, false));
                    }
                }
                else //if product uses a loan cycle
                {
                    decimal? interestRateMin = creditProduct.InterestRateMin * 100;
                    decimal? interestRateMax = creditProduct.InterestRateMax * 100;
                    nudInterestRate.Minimum = interestRateMin.Value;
                    nudInterestRate.Maximum = interestRateMax.Value;
                    lbLoanInterestRateMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                        annualType + MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                        ServicesHelper.ConvertNullableDecimalToString(interestRateMin.Value, false),
                        MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                        ServicesHelper.ConvertNullableDecimalToString(interestRateMax.Value, false));
                    nudInterestRate.Value = interestRateMin.Value;
                }
            }
            else // if it is an existing contract
            {
                //if contract doesn't use a loan cycle
                if (credit.LoanCycle == null && credit.InterestRateMin == null && credit.InterestRateMax == null)
                {
                    try
                    {
                        decimal? interestRateMin = creditProduct.InterestRateMin * 100;
                        decimal? interestRateMax = creditProduct.InterestRateMax * 100;
                        nudInterestRate.Minimum = interestRateMin.Value;
                        nudInterestRate.Maximum = interestRateMax.Value;
                        lbLoanInterestRateMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                            annualType + MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRateMin.Value, false),
                            MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRateMax.Value, false));
                        nudInterestRate.Value = credit.InterestRate * 100;
                    }
                    catch
                    {
                        nudInterestRate.Minimum = nudInterestRate.Maximum = credit.InterestRate * 100;
                        nudInterestRate.Value = credit.InterestRate * 100;
                    }

                }
                else //contract uses a loan cycle
                {
                    try
                    {
                        decimal? interestRateMin = creditProduct.InterestRateMin * 100;
                        decimal? interestRateMax = creditProduct.InterestRateMax * 100;
                        nudInterestRate.Minimum = interestRateMin.Value;
                        nudInterestRate.Maximum = interestRateMax.Value;
                        lbLoanInterestRateMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                            annualType + MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRateMin.Value, false),
                            MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                            ServicesHelper.ConvertNullableDecimalToString(interestRateMax.Value, false));
                        nudInterestRate.Value = credit.InterestRate * 100;
                    }
                    catch
                    {
                        nudInterestRate.Minimum = nudInterestRate.Maximum = credit.InterestRate * 100;
                        nudInterestRate.Value = credit.InterestRate * 100;
                    }
                }
            }
        }

        private DoubleValueRange _guaranteeFeesValueRange;
        private DecimalValueRange _amountValueRange;
        private void InitializeAmount(Loan credit, bool pForCreation)
        {
            if (pForCreation) //if it is new contract
            {
                nudLoanAmount.DecimalPlaces = (credit.Product.Currency.UseCents || _credit.UseCents) ? 2 : 0;
                if (!credit.Product.UseLoanCycle) //If product doesn't use any loan cycle
                {
                    if (!credit.Product.Amount.HasValue) //if credit amount is a range value
                    {
                        try
                        {
                            _amountValueRange = new DecimalValueRange(credit.Product.AmountMin, credit.Product.AmountMax);
                            nudLoanAmount.Enabled = true;
                            labelLoanAmountMinMax.SetRangeText(_amountValueRange.Min, _amountValueRange.Max);
                            nudLoanAmount.Minimum = _amountValueRange.Min.Value;
                            nudLoanAmount.Maximum = _amountValueRange.Max.Value;
                            nudLoanAmount.Value = _amountValueRange.Min.Value;
                        }
                        catch
                        {
                            nudLoanAmount.Minimum = nudLoanAmount.Maximum = credit.Amount.Value;
                            nudLoanAmount.Value = credit.Amount.Value;
                        }
                    }
                    else //if credit amount is a fixed value
                    {
                        try
                        {
                            _amountValueRange = new DecimalValueRange(credit.Product.Amount);
                            OCurrency valueCurrency = _amountValueRange.Value;
                            decimal value = valueCurrency.Value;
                            labelLoanAmountMinMax.SetRangeText(valueCurrency);
                            nudLoanAmount.Minimum = value;
                            nudLoanAmount.Maximum = value;
                            nudLoanAmount.Value = value;
                            nudLoanAmount.Enabled = false;
                        }
                        catch
                        {
                            nudLoanAmount.Minimum = nudLoanAmount.Maximum = credit.Amount.Value;
                            nudLoanAmount.Value = credit.Amount.Value;
                        }
                    }
                }
                else //if product uses loan cycles
                {
                    _amountValueRange = new DecimalValueRange(credit.Product.AmountMin, credit.Product.AmountMax);
                    nudLoanAmount.Enabled = true;
                    labelLoanAmountMinMax.SetRangeText(_amountValueRange.Min, _amountValueRange.Max);
                    nudLoanAmount.Minimum = _amountValueRange.Min.Value;
                    nudLoanAmount.Maximum = _amountValueRange.Max.Value;
                    nudLoanAmount.Value = _amountValueRange.Min.Value;
                }
            }
            else //if it is an existing contract
            {
                nudLoanAmount.DecimalPlaces = (credit.Product.Currency.UseCents) ? 2 : 0;

                if (credit.LoanCycle == null && !credit.AmountMin.HasValue && !credit.AmountMax.HasValue)//if contract doesn't use any loan cycles
                {
                    if (credit.Product.Amount.HasValue)//if credit amount is a fixed value
                    {
                        try
                        {
                            _amountValueRange = new DecimalValueRange(credit.Product.Amount);
                            labelLoanAmountMinMax.SetRangeText(_amountValueRange.Value);
                            nudLoanAmount.Minimum = nudLoanAmount.Maximum = _amountValueRange.Value.Value;
                        }
                        catch (Exception)
                        {
                            nudLoanAmount.Minimum = nudLoanAmount.Maximum = credit.Amount.Value;
                            nudLoanAmount.Value = credit.Amount.Value;
                        }

                    }
                    else //if credit amount is range vale
                    {
                        try
                        {
                            _amountValueRange = new DecimalValueRange(credit.Product.AmountMin, credit.Product.AmountMax);
                            labelLoanAmountMinMax.SetRangeText(_amountValueRange.Min, _amountValueRange.Max);
                            nudLoanAmount.Minimum = _amountValueRange.Min.Value;
                            nudLoanAmount.Maximum = _amountValueRange.Max.Value;
                            nudLoanAmount.Value = credit.Amount.Value;
                        }
                        catch
                        {
                            nudLoanAmount.Minimum = nudLoanAmount.Maximum = credit.Amount.Value;
                            nudLoanAmount.Value = credit.Amount.Value;
                        }
                    }
                }
                else //if product  uses loan cycles
                {
                    try
                    {
                        _amountValueRange = new DecimalValueRange(credit.AmountMin, credit.AmountMax);
                        nudLoanAmount.Enabled = true;
                        labelLoanAmountMinMax.SetRangeText(_amountValueRange.Min, _amountValueRange.Max);
                        nudLoanAmount.Minimum = _amountValueRange.Min.Value;
                        nudLoanAmount.Maximum = _amountValueRange.Max.Value;
                        nudLoanAmount.Value = credit.Amount.Value;
                    }
                    catch
                    {
                        nudLoanAmount.Minimum = nudLoanAmount.Maximum = credit.Amount.Value;
                        nudLoanAmount.Value = credit.Amount.Value;
                    }
                }
            }

            nudLoanAmount.Text = nudLoanAmount.Value.ToString(); //Workaround for text emptyness
        }

        private void InitializePackageGracePeriod(LoanProduct pPackage, bool pForCreation)
        {
            if (!pPackage.GracePeriod.HasValue) //Min and Max
            {
                numericUpDownLoanGracePeriod.Enabled = true;
                if (pForCreation)
                {
                    numericUpDownLoanGracePeriod.Minimum = pPackage.GracePeriodMin.Value;
                    numericUpDownLoanGracePeriod.Maximum = pPackage.GracePeriodMax.Value;
                }
                labelLoanGracePeriodMinMax.Text = string.Format("{0}{1}\r\n{2}{3}",
                                MultiLanguageStrings.GetString(Ressource.CreditContractForm, "min.Text"),
                                pPackage.GracePeriodMin.Value,
                                MultiLanguageStrings.GetString(Ressource.CreditContractForm, "max.Text"),
                                pPackage.GracePeriodMax.Value);
                if (!pForCreation) numericUpDownLoanGracePeriod.Value = _credit.GracePeriod.Value;
            }
            else //value
            {
                numericUpDownLoanGracePeriod.Enabled = false;
                numericUpDownLoanGracePeriod.Value = pForCreation ? pPackage.GracePeriod.Value : _credit.GracePeriod.Value;
            }
        }

        private void buttonAddContract_Click(object sender, EventArgs e)
        {
            if (_client == _group)
            {
                if (_group.GetNumberOfMembers != 0)
                {
                    FillDropDownMenuWithPackages(contextMenuStripPackage);
                    contextMenuStripPackage.Show(buttonProjectAddContract, 0 - contextMenuStripPackage.Size.Width, 0);
                }
            }
            else
            {
                FillDropDownMenuWithPackages(contextMenuStripPackage);
                contextMenuStripPackage.Show(buttonProjectAddContract, 0 - contextMenuStripPackage.Size.Width, 0);
            }
            lblLoanStatus.Visible = false;
        }

        private void buttonSaveProject_Click(object sender, EventArgs e)
        {
            try
            {
                SaveProject();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void DisplayProjects(IEnumerable<Project> pProject)
        {
            if (_oClientType == OClientTypes.Person) _personUserControl.DisplayProjects(pProject);
            if (_oClientType == OClientTypes.Group) _groupUserControl.DisplayProjects(pProject);
        }

        private void DisplaySavings(IEnumerable<ISavingsContract> pSaving)
        {
            if (_oClientType == OClientTypes.Person) _personUserControl.DisplaySavings(pSaving);
            if (_oClientType == OClientTypes.Group) _groupUserControl.DisplaySavings(pSaving);
            if (_oClientType == OClientTypes.Corporate) _corporateUserControl.DisplaySavings(pSaving);
        }

        private void SaveProject()
        {
            IClient client = null;
            if (_oClientType == OClientTypes.Corporate) client = _corporateUserControl.Corporate;
            if (_oClientType == OClientTypes.Person) client = _personUserControl.Person;
            if (_oClientType == OClientTypes.Group) client = _groupUserControl.Group;

            if (client != null)
                if (client.Id != 0)
                {
                    if (_project.Id == 0)
                    {
                        Project newProject = new Project("TEST");
                        newProject.Name = textBoxProjectName.Text;
                        newProject.Aim = textBoxProjectAim.Text;
                        newProject.BeginDate = dateTimePickerProjectBeginDate.Value;
                        newProject.Abilities = textBoxProjectAbilities.Text;
                        newProject.Experience = textBoxProjectExperience.Text;
                        newProject.Market = textBoxProjectMarket.Text;
                        newProject.Concurrence = textBoxProjectConcurrence.Text;
                        newProject.Purpose = textBoxProjectPurpose.Text;
                        newProject.ClientCode = string.Format("{0}/{1}", client.NbOfProjects, client.Id);
                        newProject.CorporateName = tBProjectCorporateName.Text;
                        newProject.CorporateSIRET = tBProjectCorporateSIRET.Text;
                        newProject.CorporateJuridicStatus = cBProjectJuridicStatus.SelectedItem == null ? "-" : cBProjectJuridicStatus.SelectedItem.ToString();
                        newProject.CorporateFiscalStatus = cBProjectFiscalStatus.SelectedItem == null ? "-" : cBProjectFiscalStatus.SelectedItem.ToString();
                        newProject.CorporateCA = ServicesHelper.ConvertStringToNullableDecimal(tBProjectCA.Text, -1);
                        newProject.CorporateNbOfJobs = (int)numericUpDownProjectJobs.Value;
                        newProject.CorporateFinancialPlanType = cBProjectFinancialPlanType.SelectedItem == null ? "-" : cBProjectFinancialPlanType.SelectedItem.ToString();
                        newProject.CorporateFinancialPlanAmount = ServicesHelper.ConvertStringToNullableDecimal(tBProjectFinancialPlanAmount.Text, -1);
                        newProject.CorporateFinancialPlanTotalAmount = ServicesHelper.ConvertStringToNullableDecimal(tBProjectFinancialPlanTotal.Text, -1);
                        newProject.CorporateRegistre = cBProjectAffiliation.Text;

                        newProject.ZipCode = projectAddressUserControl.ZipCode;
                        newProject.HomeType = projectAddressUserControl.HomeType;
                        newProject.Email = projectAddressUserControl.Email;
                        newProject.District = projectAddressUserControl.District;
                        newProject.City = projectAddressUserControl.City;
                        newProject.Address = projectAddressUserControl.Comments;
                        newProject.HomePhone = projectAddressUserControl.HomePhone;
                        newProject.PersonalPhone = projectAddressUserControl.PersonalPhone;
                        newProject.FollowUps = _followUpList;

                        newProject.Id = ServicesProvider.GetInstance().GetProjectServices().SaveProject(newProject, client);

                        _project = ServicesProvider.GetInstance().GetProjectServices().FindProjectById(newProject.Id);
                        client.AddProject(_project);

                    }
                    else
                    {
                        _project.Name = textBoxProjectName.Text;
                        _project.Aim = textBoxProjectAim.Text;
                        _project.BeginDate = dateTimePickerProjectBeginDate.Value;
                        _project.Abilities = textBoxProjectAbilities.Text;
                        _project.Experience = textBoxProjectExperience.Text;
                        _project.Market = textBoxProjectMarket.Text;
                        _project.Concurrence = textBoxProjectConcurrence.Text;
                        _project.Purpose = textBoxProjectPurpose.Text;
                        _project.FollowUps = _followUpList;

                        _project.CorporateName = tBProjectCorporateName.Text;
                        _project.CorporateSIRET = tBProjectCorporateSIRET.Text;
                        _project.CorporateJuridicStatus = cBProjectJuridicStatus.SelectedItem == null ? "-" : cBProjectJuridicStatus.SelectedItem.ToString();
                        _project.CorporateFiscalStatus = cBProjectFiscalStatus.SelectedItem == null ? "-" : cBProjectFiscalStatus.SelectedItem.ToString();
                        _project.CorporateCA = ServicesHelper.ConvertStringToNullableDecimal(tBProjectCA.Text, -1);
                        _project.CorporateNbOfJobs = (int)numericUpDownProjectJobs.Value;
                        _project.CorporateFinancialPlanType = cBProjectFinancialPlanType.SelectedItem == null ? "-" : cBProjectFinancialPlanType.SelectedItem.ToString();
                        _project.CorporateFinancialPlanAmount = ServicesHelper.ConvertStringToNullableDecimal(tBProjectFinancialPlanAmount.Text, -1);
                        _project.CorporateFinancialPlanTotalAmount = ServicesHelper.ConvertStringToNullableDecimal(tBProjectFinancialPlanTotal.Text, -1);
                        _project.CorporateRegistre = cBProjectAffiliation.Text;

                        _project.ZipCode = projectAddressUserControl.ZipCode;
                        _project.HomeType = projectAddressUserControl.HomeType;
                        _project.Email = projectAddressUserControl.Email;
                        _project.District = projectAddressUserControl.District;
                        _project.City = projectAddressUserControl.City;
                        _project.Address = projectAddressUserControl.Comments;
                        _project.HomePhone = projectAddressUserControl.HomePhone;
                        _project.PersonalPhone = projectAddressUserControl.PersonalPhone;


                        ServicesProvider.GetInstance().GetProjectServices().SaveProject(_project, client);
                    }
                    DisplayProjects(client.Projects);
                    DisplaySavings(client.Savings);
                    DisplaySelectedProject(_project);
                }
                else
                {
                    MessageBox.Show("Please, save client before!");
                }
        }

        private void ReInitializeListViewInstallment()
        {
            listViewLoanInstallments.Items.Clear();
        }

        private void CheckAmount()
        {
            if (null == _credit) return;
            try
            {
                OCurrency amount = ServicesHelper.ConvertStringToDecimal(nudLoanAmount.Text, 0, _credit.Product.UseCents);

                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_amountValueRange.Min, _amountValueRange.Max, amount))
                {
                    btnSaveLoan.Enabled = false;
                    tabControlPerson.SelectTab(tabPageLoansDetails);
                    nudLoanAmount.Focus();
                }
                else
                {
                    btnSaveLoan.Enabled = true;
                }
                ReInitializeListViewInstallment();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void textBoxLoanAnticipatedFees_Leave(object sender, EventArgs e)
        {
            CheckAnticipatedFees(1);
        }

        private void CheckAnticipatedFees(int anticipatedFeesType)
        {
            try
            {
                if (anticipatedFeesType == 1)
                {
                    textBoxLoanAnticipatedTotalFees.BackColor = Color.White;
                    btnSaveLoan.Enabled = true;

                    double anticipatedTotalFees =
                        ServicesHelper.ConvertStringToDouble(textBoxLoanAnticipatedTotalFees.Text, true);

                    if (
                        !ServicesHelper.CheckIfValueBetweenMinAndMax(_anticipatedTotalFeesValueRange.Min,
                                                                     _anticipatedTotalFeesValueRange.Max,
                                                                     anticipatedTotalFees))
                    {
                        textBoxLoanAnticipatedTotalFees.BackColor = Color.Red;
                        btnSaveLoan.Enabled = false;
                        textBoxLoanAnticipatedTotalFees.Focus();
                    }
                }
                else
                {
                    tbLoanAnticipatedPartialFees.BackColor = Color.White;
                    btnSaveLoan.Enabled = true;

                    double anticipatedPartialFees =
                        ServicesHelper.ConvertStringToDouble(tbLoanAnticipatedPartialFees.Text, true);

                    if (
                        !ServicesHelper.CheckIfValueBetweenMinAndMax(_anticipatedPartialFeesValueRange.Min,
                                                                     _anticipatedPartialFeesValueRange.Max,
                                                                     anticipatedPartialFees))
                    {
                        tbLoanAnticipatedPartialFees.BackColor = Color.Red;
                        tbLoanAnticipatedPartialFees.Focus();
                        btnSaveLoan.Enabled = false;
                    }
                }

                ReInitializeListViewInstallment();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void textBoxLoanLateFeesOnAmount_Leave(object sender, EventArgs e)
        {
            CheckLateFeesOnAmount();
        }

        private void CheckLateFeesOnAmount()
        {
            try
            {
                textBoxLoanLateFeesOnAmount.BackColor = Color.White;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnAmount.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnAmountRangeValue.Min, _lateFeesOnAmountRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnAmount.BackColor = Color.Red;
                    textBoxLoanLateFeesOnAmount.Focus();
                }
                ReInitializeListViewInstallment();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void textBoxLoanLateFeesOnOverduePrincipal_Leave(object sender, EventArgs e)
        {
            CheckLateFeesOnOverduePrincipal();
        }

        private void CheckLateFeesOnOverduePrincipal()
        {
            try
            {
                textBoxLoanLateFeesOnOverduePrincipal.BackColor = Color.White;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnOverduePrincipal.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnOverduePrincipalRangeValue.Min, _lateFeesOnOverduePrincipalRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnOverduePrincipal.BackColor = Color.Red;
                    textBoxLoanLateFeesOnOverduePrincipal.Focus();

                }
                ReInitializeListViewInstallment();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void textBoxLoanLateFeesOnOLB_Leave(object sender, EventArgs e)
        {
            CheckLateFeesOnOlb();
        }

        private void CheckLateFeesOnOlb()
        {
            try
            {
                textBoxLoanLateFeesOnOLB.BackColor = Color.White;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnOLB.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnOLBRangeValue.Min, _lateFeesOnOLBRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnOLB.BackColor = Color.Red;
                    textBoxLoanLateFeesOnOLB.Focus();
                }
                ReInitializeListViewInstallment();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void textBoxLoanLateFeesOnOverdueInterest_Leave(object sender, EventArgs e)
        {
            CheckLateFeesOnOverdueInterest();
        }

        private void CheckLateFeesOnOverdueInterest()
        {
            try
            {
                textBoxLoanLateFeesOnOverdueInterest.BackColor = Color.White;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnOverdueInterest.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnOverdueInterestRangeValue.Min, _lateFeesOnOverdueInterestRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnOverdueInterest.BackColor = Color.Red;
                    textBoxLoanLateFeesOnOverdueInterest.Focus();
                }
                ReInitializeListViewInstallment();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }


        private void buttonLoanPreview_Click(object sender, EventArgs e)
        {
            Preview();
        }

        private Loan CreateLoan()
        {
            Loan credit = new Loan(_product,
                                       ServicesHelper.ConvertStringToDecimal(nudLoanAmount.Text, _product.UseCents),
                                       nudInterestRate.Value / 100m,
                                       Convert.ToInt32(nudLoanNbOfInstallments.Value),
                                       Convert.ToInt32(numericUpDownLoanGracePeriod.Value),
                                       dateLoanStart.Value,
                                       dtpDateOfFirstInstallment.Value,
                                       User.CurrentUser,
                                       ServicesProvider.GetInstance().GetGeneralSettings(),
                                       ServicesProvider.GetInstance().GetNonWorkingDate(),
                                       CoreDomainProvider.GetInstance().GetProvisioningTable(),
                                       CoreDomainProvider.GetInstance().GetChartOfAccounts())
                              {
                                  Guarantors = _listGuarantors,
                                  Collaterals = _collaterals,
                                  LoanShares = _loanShares
                              };

            if (!string.IsNullOrEmpty(tbLocAmount.Text))
                credit.AmountUnderLoc = decimal.Parse(tbLocAmount.Text);

            if (textBoxLoanContractCode.Text != "") credit.Code = textBoxLoanContractCode.Text;

            _toChangeAlignDate = false;
            credit.FirstInstallmentDate = dtpDateOfFirstInstallment.Value;
            _toChangeAlignDate = true;

            credit.InstallmentType =
                ServicesProvider.GetInstance().GetProductServices().FindInstallmentType(
                    (int)comboBoxLoanInstallmentType.Tag);

            _firstInstallmentDate = dtpDateOfFirstInstallment.Value;

            credit.AlignDisbursementDate = credit.CalculateAlignDisbursementDate(_firstInstallmentDate);

            credit.AnticipatedTotalRepaymentPenalties = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanAnticipatedTotalFees.Text, true, -1).Value;
            credit.AnticipatedPartialRepaymentPenalties = ServicesHelper.ConvertStringToNullableDouble(tbLoanAnticipatedPartialFees.Text, true, -1).Value;
            credit.NonRepaymentPenalties.InitialAmount = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnAmount.Text, true, -1).Value;
            credit.NonRepaymentPenalties.OLB = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnOLB.Text, true, -1).Value;
            credit.NonRepaymentPenalties.OverDueInterest = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnOverdueInterest.Text, true, -1).Value;
            credit.NonRepaymentPenalties.OverDuePrincipal = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnOverduePrincipal.Text, true, -1).Value;
            credit.GracePeriod = Convert.ToInt32(numericUpDownLoanGracePeriod.Value);
            credit.GracePeriodOfLateFees = Convert.ToInt32(_gracePeriodOfLateFees);

            credit.FundingLine = comboBoxLoanFundingLine.Tag != null ? (FundingLine)comboBoxLoanFundingLine.Tag : null;

            credit.LoanOfficer = (User)cmbLoanOfficer.SelectedItem;

            credit.LoanPurpose = textBoxLoanPurpose.Text;
            credit.Comments = textBoxComments.Text;

            credit.CompulsorySavings = GetSelectedSavingProduct();
            credit.CompulsorySavingsPercentage = (int)numCompulsoryAmountPercent.Value;
            credit.LoanEntryFeesList = new List<LoanEntryFee>();
            foreach (ListViewItem item in lvEntryFees.Items)
            {
                if (item.Tag is LoanEntryFee)
                {
                    ((LoanEntryFee)item.Tag).FeeValue = decimal.Parse(item.SubItems[1].Text);
                    credit.LoanEntryFeesList.Add((LoanEntryFee)item.Tag);
                }
            }
            if (credit.Product.CycleId != null)
            {
                credit.AmountMin = _product.AmountMin;
                credit.AmountMax = _product.AmountMax;
                credit.InterestRateMin = _product.InterestRateMin;
                credit.InterestRateMax = _product.InterestRateMax;
                credit.NmbOfInstallmentsMin = _product.NbOfInstallmentsMin;
                credit.NmbOfInstallmentsMax = _product.NbOfInstallmentsMax;
                credit.LoanCycle = _client.LoanCycle;
            }

            credit.Insurance = decimal.Parse(tbInsurance.Text);
            if (_credit != null && _credit.ScheduleChangedManually)
            {
                credit.ScheduleChangedManually = _credit.ScheduleChangedManually;
                credit.InstallmentList = _credit.InstallmentList;
            }
            credit.EconomicActivity = eacLoan.Activity;
            return credit;
        }

        private Loan CreateAndSetContract()
        {
            if (_credit == null)
            {
                _credit = CreateLoan();
            }
            else if (_credit.Id == 0)
            {
                _credit = CreateLoan();
            }
            else
            {
                _credit.Guarantors = _listGuarantors;
                _credit.Collaterals = _collaterals;
                _credit.LoanShares = _loanShares;
                _credit.Amount = ServicesHelper.ConvertStringToDecimal(nudLoanAmount.Text, _credit.UseCents);
                _credit.StartDate = dateLoanStart.Value;
                _credit.FirstInstallmentDate = dtpDateOfFirstInstallment.Value;
                dtpDateOfFirstInstallment.Value = _credit.FirstInstallmentDate;
                _credit.InterestRate = ServicesHelper.ConvertStringToNullableDecimal(nudInterestRate.Text, true, -1).Value;
                _credit.NbOfInstallments = Convert.ToInt32(nudLoanNbOfInstallments.Value);
                _credit.InstallmentType = ServicesProvider.GetInstance().GetProductServices().FindInstallmentType((int)comboBoxLoanInstallmentType.Tag);
                _credit.AmountUnderLoc = ServicesHelper.ConvertStringToDecimal(tbLocAmount.Text, _credit.UseCents);
                if (_credit.ContractStatus == OContractStatus.Pending)
                {
                    _credit.AlignDisbursementDate = _credit.CalculateAlignDisbursementDate(_credit.FirstInstallmentDate);
                }

                _credit.AnticipatedTotalRepaymentPenalties = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanAnticipatedTotalFees.Text, true, -1).Value;
                _credit.AnticipatedPartialRepaymentPenalties = ServicesHelper.ConvertStringToNullableDouble(tbLoanAnticipatedPartialFees.Text, true, -1).Value;
                _credit.NonRepaymentPenalties.InitialAmount = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnAmount.Text, true, -1).Value;
                _credit.NonRepaymentPenalties.OLB = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnOLB.Text, true, -1).Value;
                _credit.NonRepaymentPenalties.OverDueInterest = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnOverdueInterest.Text, true, -1).Value;
                _credit.NonRepaymentPenalties.OverDuePrincipal = ServicesHelper.ConvertStringToNullableDouble(textBoxLoanLateFeesOnOverduePrincipal.Text, true, -1).Value;
                _credit.GracePeriod = Convert.ToInt32(numericUpDownLoanGracePeriod.Value);
                _credit.GracePeriodOfLateFees = _gracePeriodOfLateFees;

                _credit.LoanOfficer = (User)cmbLoanOfficer.SelectedItem;

                _credit.FundingLine = comboBoxLoanFundingLine.Tag != null
                                         ? (FundingLine)comboBoxLoanFundingLine.Tag
                                         : null;

                _credit.LoanInitialOfficer = _credit.LoanOfficer;

                _credit.LoanPurpose = textBoxLoanPurpose.Text;
                _credit.Comments = textBoxComments.Text;

                _credit.CompulsorySavings = GetSelectedSavingProduct();
                _credit.CompulsorySavingsPercentage = (int)numCompulsoryAmountPercent.Value;
                _credit.Insurance = decimal.Parse(tbInsurance.Text);

                _credit.LoanEntryFeesList = new List<LoanEntryFee>();
                foreach (ListViewItem item in lvEntryFees.Items)
                {
                    if (item.Tag is LoanEntryFee)
                    {
                        ((LoanEntryFee)item.Tag).FeeValue = decimal.Parse(item.SubItems[1].Text);
                        _credit.LoanEntryFeesList.Add((LoanEntryFee)item.Tag);
                    }
                }
                _credit.EconomicActivity = eacLoan.Activity;
            }

            return _credit;
        }

        private Loan Preview()
        {
            try
            {
                Loan credit = CreateAndSetContract();
                ServicesProvider.GetInstance().GetContractServices().CheckLoanFilling(credit);
                DisplayInstallments(ref credit);
                return credit;
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }

            return null;
        }

        private void DisplayInstallments(ref Loan pCredit)
        {
            listViewLoanInstallments.Items.Clear();

            if (pCredit.InstallmentList.Count == 0)
            {
                pCredit.Product = _product;
            }

            if (!pCredit.Disbursed)
            {
                if (!pCredit.ScheduleChangedManually)
                {
                    pCredit.InstallmentList = pCredit.CalculateInstallments(true);
                    pCredit.CalculateStartDates();
                }
            }

            OCurrency interestTotal = 0;
            OCurrency principalTotal = 0;

            foreach (Installment installment in pCredit.InstallmentList)
            {
                ListViewItem listViewItem = new ListViewItem(installment.Number.ToString());

                if (!_useGregorienCalendar)
                {
                    DateTime dt = new DateTime(installment.ExpectedDate.Year, installment.ExpectedDate.Month,
                                     installment.ExpectedDate.Day,
                                     currentCalendar);
                    listViewItem.SubItems.Add(string.Format("{0}/{1}/{2}", targetCalendar.GetDayOfMonth(dt), targetCalendar.GetMonth(dt), targetCalendar.GetYear(dt)));
                }
                else
                    listViewItem.SubItems.Add(installment.ExpectedDate.ToShortDateString());

                listViewItem.SubItems.Add(installment.InterestsRepayment.GetFormatedValue(pCredit.UseCents));
                listViewItem.SubItems.Add(installment.CapitalRepayment.GetFormatedValue(pCredit.UseCents));
                listViewItem.SubItems.Add(installment.Amount.GetFormatedValue(pCredit.UseCents));
                listViewItem.SubItems.Add(ServicesProvider.GetInstance().GetGeneralSettings().IsOlbBeforeRepayment
                                              ? installment.OLB.GetFormatedValue(pCredit.UseCents)
                                              : installment.OLBAfterRepayment.GetFormatedValue(pCredit.UseCents));
                listViewLoanInstallments.Items.Add(listViewItem);
                interestTotal += installment.InterestsRepayment;
                principalTotal += installment.CapitalRepayment;
            }
            // Add totals to the list view
            ListViewItem total = new ListViewItem
                                     {
                                         Font = new Font(listViewLoanInstallments.Font, FontStyle.Bold)
                                     };
            total.SubItems.Add("");
            total.SubItems.Add(interestTotal.GetFormatedValue(pCredit.UseCents));
            total.SubItems.Add(principalTotal.GetFormatedValue(pCredit.UseCents));
            total.SubItems.Add((interestTotal + principalTotal).GetFormatedValue(pCredit.UseCents));
            listViewLoanInstallments.Items.Add(total);
        }

        private void buttonLoanSave_Click(object sender, EventArgs e)
        {
            if (!btnSaveLoan.Enabled || !btnUpdateSettings.Enabled)
            {
                if (!btnSaveLoan.Enabled)
                    tabControlPerson.SelectedTab = tabPageLoansDetails;
                else if (!btnUpdateSettings.Enabled)
                    tabControlPerson.SelectedTab = tabPageAdvancedSettings;
                Fail(ML.GetString(Ressource.ClientForm, "ParametersAreWrong"));
                return;
            }

            _credit = Preview();

            // Compulsory savings
            if (_credit != null)
            {
                if (_credit.Product.UseCompulsorySavings && sender == btnUpdateSettings)
                {
                    if (_credit.CompulsorySavings == null)
                    {
                        Fail(ML.GetString(Ressource.ClientForm, "CumpolsorySavingIsMandatory"));
                        return;
                    }
                }

                if (_group != null && !AreSharesUpToDate())
                {
                    // Opening loan shares

                    //update loan shares from database
                    if (_credit.Id > 0 && 0 == _loanShares.Count)
                        _loanShares = ServicesProvider.GetInstance().GetContractServices().GetLoanShares(_credit.Id);

                    OCurrency sum = _loanShares.Sum(item => item.Amount.Value);
                    if (_loanShares.Count == 0 || sum != _credit.Amount)
                        InitializeLoanShares();

                    _credit.LoanShares = _loanShares;

                    var frm = new LoanSharesForm(_credit, _group);
                    if (DialogResult.OK != frm.ShowDialog())
                        return;
                }
            }

            if (_credit != null)
                SaveContract();

            _copyOfCredit.Id = _credit != null ? _credit.Id : 0;
        }

        private void DisplayListViewLoanRepayments(Loan credit)
        {
            lvLoansRepayments.Items.Clear();
            OCurrency OLBDue = 0;
            OCurrency interestsDue = 0;

            foreach (Installment installment in credit.InstallmentList)
            {
                var listViewItem = new ListViewItem(installment.Number.ToString());

                // late installation mark as red
                if ((installment.CapitalRepayment + installment.InterestsRepayment >
                    installment.PaidCapital + installment.PaidInterests) || !installment.IsRepaid)
                {
                    if (installment.PaidDate.HasValue)
                    {
                        int lateDays = Convert.ToInt32(installment.PaidDate.Value.Subtract(installment.ExpectedDate).TotalDays);
                        if (Math.Abs(lateDays) > 0)
                        {
                            listViewItem.BackColor = Color.White;
                            listViewItem.ForeColor = Color.Red;
                        }
                    }
                    else
                    {
                        DateTime now = TimeProvider.Now;
                        DateTime dt = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, 0);
                        int lateDays = Convert.ToInt32(dt.Subtract(installment.ExpectedDate).TotalDays);
                        if (lateDays > 0)
                        {
                            listViewItem.BackColor = Color.White;
                            listViewItem.ForeColor = Color.Red;
                        }
                    }
                }

                if (installment.IsRepaid)
                {
                    listViewItem.BackColor = Color.FromArgb(61, 153, 57);
                    listViewItem.ForeColor = Color.White;
                }
                else
                {
                    if (!installment.IsRepaid && OLBDue.Value == 0)
                        OLBDue = installment.OLB;

                    interestsDue += installment.InterestsRepayment - installment.PaidInterests;
                }

                if (installment.IsPending)
                {
                    listViewItem.BackColor = Color.Orange;
                    listViewItem.ForeColor = Color.White;
                }

                listViewItem.Tag = installment;
                listViewItem.SubItems.Add(installment.ExpectedDate.ToShortDateString());
                listViewItem.SubItems.Add(installment.InterestsRepayment.GetFormatedValue(_credit.UseCents));
                listViewItem.SubItems.Add(installment.CapitalRepayment.GetFormatedValue(_credit.UseCents));
                listViewItem.SubItems.Add(installment.AmountHasToPayWithInterest.GetFormatedValue(_credit.UseCents));

                if (ServicesProvider.GetInstance().GetGeneralSettings().IsOlbBeforeRepayment)
                    listViewItem.SubItems.Add(installment.OLB.GetFormatedValue(_credit.UseCents));
                else
                    listViewItem.SubItems.Add(installment.OLBAfterRepayment.GetFormatedValue(_credit.UseCents));


                if (installment.PaidInterests == 0)
                    listViewItem.SubItems.Add("-");
                else
                    listViewItem.SubItems.Add(installment.PaidInterests.GetFormatedValue(_credit.UseCents));

                if (installment.PaidCapital == 0)
                    listViewItem.SubItems.Add("-");
                else
                    listViewItem.SubItems.Add(installment.PaidCapital.GetFormatedValue(_credit.UseCents));

                if (installment.PaidDate.HasValue)
                {
                    listViewItem.SubItems.Add(installment.PaidDate.Value.ToShortDateString());
                    int lateDays = Convert.ToInt32(installment.PaidDate.Value.Subtract(installment.ExpectedDate).TotalDays);
                    if (lateDays > 0)
                    {
                        listViewItem.SubItems.Add(lateDays.ToString());
                    }
                    else
                    {
                        listViewItem.SubItems.Add("-");
                    }
                }
                else
                {
                    listViewItem.SubItems.Add("-");

                    if (installment.Number <= credit.GracePeriod)
                    {
                        listViewItem.SubItems.Add("-");
                    }
                    else
                    {
                        DateTime now = TimeProvider.Now;
                        DateTime dt = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, 0);
                        int lateDays = Convert.ToInt32(dt.Subtract(installment.ExpectedDate).TotalDays);
                        if (lateDays > 0) listViewItem.SubItems.Add(lateDays.ToString());
                        else listViewItem.SubItems.Add("-");
                    }
                }

                listViewItem.SubItems.Add(installment.Comment);

                lvLoansRepayments.Items.Add(listViewItem);
            }

            richTextBoxStatus.Clear();

            String statusText = MultiLanguageStrings.GetString(Ressource.ClientForm, "Status.Text") + "\n" +
                               MultiLanguageStrings.GetString(Ressource.ClientForm, "Currency.Text") + "   " + _credit.Product.Currency.Name + "\n" +
                               MultiLanguageStrings.GetString(Ressource.ClientForm, "CapitalDue.Text") + "   "
                               + _credit.CalculateActualOlb().GetFormatedValue(credit.UseCents) + "\n" +
                               MultiLanguageStrings.GetString(Ressource.ClientForm, "PercentsDue.Text") + "   " + interestsDue.GetFormatedValue(credit.UseCents);
            richTextBoxStatus.Text = statusText;
        }

        private Exception SaveContract()
        {
            if (_credit.FundingLine == null && comboBoxLoanFundingLine.Tag == null)
            {
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractIsReadOnly.Text"));
                return null;
            }
            try
            {
                IClient client = null;
                switch (_oClientType)
                {
                    case OClientTypes.Corporate:
                        client = _corporateUserControl.Corporate;
                        break;

                    case OClientTypes.Person:
                        client = _personUserControl.Person;
                        break;

                    case OClientTypes.Group:
                        client = _groupUserControl.Group;
                        break;
                }

                if (OClientTypes.Group == _oClientType && 0 == _groupUserControl.Group.Members.Count)
                {
                    string text = MultiLanguageStrings.GetString(Ressource.ClientForm, "GroupEmptyWarning.Text");
                    string caption = MultiLanguageStrings.GetString(Ressource.ClientForm, "GroupEmptyWarning.Caption");
                    MessageBox.Show(this, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                Loan credit;
                if (_credit.Id == 0)
                {
                    _customizableLoanFieldsControl.Check();
                    if (tabControlPerson.SelectedTab != tabPageAdvancedSettings)
                    {
                        tabControlPerson.SelectedTab = tabPageAdvancedSettings;
                        return null;
                    }
                    tbLocAmount.Enabled = false;
                    tbInsurance.Enabled = false;
                    credit = Preview();

                    if (credit == null)
                        return null;

                    credit.ClientType = _oClientType;
                    ServicesProvider.GetInstance().GetContractServices().SaveLoan(ref credit, _project.Id, ref client, (tx, id) =>
                    {
                        foreach (var extension in LoanExtensions) extension.Save(credit, tx);
                    });
                    _credit = credit;

                    if (client != null)
                        _project.AddCredit(_credit, _oClientType);

                    if (_credit.Id > 0)
                    {
                        _customizableLoanFieldsControl.Save(_credit.Id);
                    }

                    InitLoanDetails(false, false, false);
                    InitializeTabPageGuaranteesDetailsButtons(_credit.Product.UseGuarantorCollateral);

                    DisplayContracts(_project.Credits);

                    tabControlPerson.TabPages.Add(tabPageCreditCommitee);
                    EnableBoxCreditCommitteeComponents(true);

                    SetCcEnabled(true);
                    textBoxCreditCommiteeComment.Text = "";

                    tabControlPerson.SelectedTab = _product.UseGuarantorCollateral ? tabPageLoanGuarantees : tabPageCreditCommitee;

                    Text = string.Format("{0} - {1}", _title, _credit.Code);
                    ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();
                }
                else
                {
                    credit = Preview();

                    if (_customizableLoanFieldsControl != null)
                        _customizableLoanFieldsControl.Check();

                    ServicesProvider.GetInstance().GetContractServices().SaveLoan(ref credit, _project.Id, ref client, (tx, id) =>
                    {
                        LoanExtensions.ForEach(e => e.Save(credit, tx));
                    });

                    if (_customizableLoanFieldsControl != null)
                    {
                        _customizableLoanFieldsControl.Save(_credit.Id);
                    }

                    if (_oClientType == OClientTypes.Group)
                    {
                        int nbOfMembers = _groupUserControl.Group.Members.Count;
                        for (int i = 0; i < nbOfMembers; i++)
                        {
                            _groupUserControl.Group.Members[i].LoanShareAmount = credit.Amount.Value / nbOfMembers;
                        }
                    }
                }

                switch (_oClientType)
                {
                    case OClientTypes.Corporate:
                        _corporateUserControl.Corporate = ServicesProvider.GetInstance().GetClientServices().FindTiers(client.Id, _oClientType) as Corporate;
                        break;

                    case OClientTypes.Person:
                        _personUserControl.Person = ServicesProvider.GetInstance().GetClientServices().FindTiers(client.Id, _oClientType) as Person;
                        break;

                    case OClientTypes.Group:
                        _groupUserControl.Group = ServicesProvider.GetInstance().GetClientServices().FindTiers(client.Id, _oClientType) as Group;
                        break;
                }

                // Automatically adjust loan share amounts for corporate members.
                if (_oClientType == OClientTypes.Group && credit.Amount != credit.GetSumOfLoanShares())
                {
                    credit.ResetLoanShares();
                    if (_groupUserControl.Group != null)
                    {
                        int nb = _groupUserControl.Group.Members.Count;
                        if (nb > 0)
                        {
                            decimal share = Math.Floor(credit.Amount.Value / nb);
                            decimal shareForLeader = credit.Amount.Value - (nb - 1) * share;
                            for (int i = 0; i < nb; i++)
                            {
                                Group group = _groupUserControl.Group;
                                Member member = group.Members[i];
                                OCurrency amount = member.IsLeader ? shareForLeader : share;
                                credit.LoanShares.Add(new LoanShare { PersonId = member.Tiers.Id, PersonName = member.Tiers.Name, Amount = amount });
                            }
                        }
                    }
                }

                if (_groupUserControl != null)
                {
                    if (_groupUserControl.Group != null)
                        ServicesProvider.GetInstance().GetContractServices().UpdateLoanShares(credit, _groupUserControl.Group.Id);
                }

                textBoxLoanContractCode.Text = _credit.Code;
                btnEditSchedule.Visible = _credit.Product.AllowFlexibleSchedule;
                btnEditSchedule.Enabled = (_credit.PendingOrPostponed() && !_credit.Disbursed);
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                return ex;
            }
            return null;
        }

        private void listViewContracts_DoubleClick(object sender, EventArgs e)
        {
            ViewContract();
        }

        private void buttonProjectViewContract_Click(object sender, EventArgs e)
        {
            ViewContract();
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult.OK == DialogResult) return;
            _person = null;
            _corporate = null;
            _client = null;

            if (_group != null)
                _group.Dispose();

            //if (_project != null)
            //    _project.Dispose();

            //_credit.Dispose();
            //DialogResult = DialogResult.Cancel;
        }

        private void buttonLoanDisbursment_Click(object sender, EventArgs e)
        {
            // Collateral & Guarantor
            if (_credit.Product.UseGuarantorCollateral)
            {
                if (!_credit.Product.SetSeparateGuarantorCollateral)
                {
                    if (_totalCollateralAmount + _totalGuarantorAmount <
                        _credit.Amount.Value * _credit.Product.PercentageTotalGuarantorCollateral / 100)
                    {
                        var message = string.Format(ML.GetString(Ressource.ClientForm, "CollateralGuarantorAmountIsNotEnough"),
                                        ServicesHelper.ConvertDecimalToString(_credit.Amount.Value * _credit.Product.PercentageTotalGuarantorCollateral / 100),
                                        _credit.Product.PercentageTotalGuarantorCollateral);
                        Fail(message);
                        return;
                    }
                }
                else
                {
                    if (_totalGuarantorAmount < _credit.Amount.Value * _credit.Product.PercentageSeparateGuarantour / 100)
                    {
                        var message = string.Format(ML.GetString(Ressource.ClientForm, "GuarantorAmountIsNotEnough"),
                                        ServicesHelper.ConvertDecimalToString(_credit.Amount.Value * _credit.Product.PercentageSeparateGuarantour / 100),
                                        _credit.Product.PercentageSeparateGuarantour);
                        Fail(message);
                        return;
                    }

                    if (_totalCollateralAmount < _credit.Amount.Value * _credit.Product.PercentageSeparateCollateral / 100)
                    {
                        var message = string.Format(ML.GetString(Ressource.ClientForm, "CollateralAmountIsNotEnough"),
                                        ServicesHelper.ConvertDecimalToString(_credit.Amount.Value * _credit.Product.PercentageSeparateCollateral / 100),
                                        _credit.Product.PercentageSeparateCollateral);
                        Fail(message);

                        return;
                    }
                }
            }

            // Compulsory savings
            if (_credit.Product.UseCompulsorySavings)
            {
                _credit.CompulsorySavings = SavingServices.GetSavingForLoan(_credit.Id, true);
                if (_credit.CompulsorySavings != null)
                {
                    decimal savingsBalance = 0;
                    decimal totalAmountPercentage = 0;
                    savingsBalance = _credit.CompulsorySavings.GetBalance().Value;

                    foreach (Loan assosiatedLoan in _credit.CompulsorySavings.Loans)
                    {
                        if (assosiatedLoan.CompulsorySavingsPercentage != null && (assosiatedLoan.ContractStatus == OContractStatus.Active ||
                            assosiatedLoan.ContractStatus == OContractStatus.Validated))
                            totalAmountPercentage += (assosiatedLoan.Amount.Value * ((decimal)assosiatedLoan.CompulsorySavingsPercentage / 100));
                    }
                    if (totalAmountPercentage > savingsBalance)
                    {
                        var message = string.Format(ML.GetString(Ressource.ClientForm, "BalanceIsNotEnough"),
                            new OCurrency(savingsBalance).GetFormatedValue(_credit.Product.UseCents),
                            new OCurrency(totalAmountPercentage).GetFormatedValue(_credit.Product.UseCents));
                        Fail(message);
                        return;
                    }
                }
                else
                {
                    Fail(MultiLanguageStrings.GetString(Ressource.ClientForm, "LoanRequiresCompulsory"));
                    return;
                }
            }

            // Group
            if (_group != null)
            {
                //update loan shares from database
                if (_credit.Id > 0 && 0 == _loanShares.Count)
                {
                    _loanShares = ServicesProvider.GetInstance().GetContractServices().GetLoanShares(_credit.Id);
                }

                _credit = Preview();

                if (_loanShares.Count == 0 || _group.Members.Count != _loanShares.Count)
                    InitializeLoanShares();
                else
                    _credit.LoanShares = _loanShares;

                if (_group != null && !AreSharesUpToDate())
                {
                    LoanSharesForm frm = new LoanSharesForm(_credit, _group);
                    if (frm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
            }

            Disburse();
        }

        private void Disburse()
        {
            try
            {
                var loanDisbursementForm = new LoanDisbursementForm(_credit);
                loanDisbursementForm.ShowDialog();
                _credit = loanDisbursementForm.Loan;

                if (_credit.Disbursed)
                {
                    MessageBox.Show(MultiLanguageStrings.GetString(Ressource.CreditContractForm, "disbuseDone.Text"), "",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                    tabControlPerson.TabPages.Add(tabPageLoanRepayment);
                    tabControlPerson.SelectedTab = tabPageLoanRepayment;
                    buttonLoanDisbursment.Enabled = false;
                    ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();
                    InitializeTabPageLoanRepayment(_credit);
                    DisplayInstallments(ref _credit);
                    DisableCommitteeDecision(_credit.ContractStatus);
                    btnEditSchedule.Visible = false;

                    IClient client = null;
                    if (_oClientType == OClientTypes.Corporate)
                    {
                        client = _corporateUserControl.Corporate;
                        client.LoanCycle++;
                        _corporateUserControl.SyncLoanCycle();
                    }
                    if (_oClientType == OClientTypes.Person)
                    {
                        client = _personUserControl.Person;
                        client.LoanCycle++;
                        _personUserControl.SyncLoanCycle();
                    }
                    if (_oClientType == OClientTypes.Group)
                    {
                        client = _groupUserControl.Group;
                        client.LoanCycle++;
                        foreach (Member member in (client as Group).Members)
                        {
                            member.Tiers.LoanCycle++;
                        }
                        _groupUserControl.SyncLoanCycle();
                    }

                    var creditCode = _credit.Code;
                    foreach (var project in client.Projects)
                        foreach (Loan loan in project.Credits)
                            if (loan.Code == creditCode)
                                loan.Disbursed = true;

                    var currLoan = _project.Credits.FirstOrDefault(c => c.Code.Equals(creditCode));
                    if (currLoan == null)
                        throw new ApplicationException("Disbursed contract not found. Contract code:" + creditCode);
                    currLoan.Disbursed = true;
                    currLoan.ContractStatus = OContractStatus.Active;
                    DisplayContracts(_project.Credits);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void buttonLoanRepaymentRepay_Click(object sender, EventArgs e)
        {
            Repay();
        }

        private void Repay()
        {
            if (_credit.HasPendingInstallment)
            {
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractHasPendingInstallment.Text"));
                return;
            }

            if (_credit.FundingLine == null && comboBoxLoanFundingLine.Tag == null)
            {
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractIsReadOnly.Text"));
                return;
            }
            try
            {
                IClient client = null;
                if (_oClientType == OClientTypes.Corporate) client = _corporateUserControl.Corporate;
                if (_oClientType == OClientTypes.Person) client = _personUserControl.Person;
                if (_oClientType == OClientTypes.Group) client = _groupUserControl.Group;

                var creditContractRepayForm = new CreditContractRepayForm(_credit, client);
                creditContractRepayForm.ShowDialog();

                if (creditContractRepayForm.DialogResult != DialogResult.Cancel)
                {
                    _credit = creditContractRepayForm.Contract;

                    if (_oClientType == OClientTypes.Group && creditContractRepayForm.EscapedMember != null)
                    {
                        foreach (Member member in ((Group)client).Members)
                        {
                            if (member.Tiers.Name == creditContractRepayForm.EscapedMember.Tiers.Name)
                            {
                                member.CurrentlyIn = false;
                                creditContractRepayForm.EscapedMember.CurrentlyIn = false;
                            }
                        }
                        _groupUserControl.Group.DeleteMember(creditContractRepayForm.EscapedMember);
                        _groupUserControl.DisplayMembers();
                    }

                    DisplayListViewLoanRepayments(_credit);
                    DisplayLoanEvents(_credit);

                    InitializeContractStatus(_credit);
                    if (_credit.Closed)
                    {
                        buttonRepay.Enabled = false;
                        //we are sure that the last credit is closed, so we force here to make the last credit closed...
                        int count = _project.Credits.Count;
                        _project.Credits[count - 1].Closed = true;
                        DisplayContracts(_project.Credits);
                    }

                    if (_credit.InstallmentList[_credit.InstallmentList.Count - 1].IsRepaid)
                    {
                        buttonLoanRepaymentRepay.Enabled = false;
                        buttonLoanReschedule.Enabled = false;
                        buttonReschedule.Enabled = false;
                        btnWriteOff.Enabled = false;
                    }

                    if (MdiParent != null)
                        ((LotrasmicMainWindowForm)MdiParent).ReloadAlertsSync();
                }
                SetAddTrancheButton(_credit);
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void DisplayGuarantors(IEnumerable<Guarantor> pGuarantors, OCurrency pAmount)
        {
            _totalGuarantorAmount = 0;
            OCurrency totalGuarantorAmountPercent = 0;
            listViewGuarantors.Items.Clear();

            foreach (Guarantor selectedGuarantor in pGuarantors)
            {
                ListViewItem listViewItem = new ListViewItem(((Person)selectedGuarantor.Tiers).Name) { Tag = selectedGuarantor };
                listViewItem.SubItems.Add(selectedGuarantor.Amount.GetFormatedValue(_credit.UseCents));

                if (pAmount.HasValue)
                    listViewItem.SubItems.Add((selectedGuarantor.Amount / pAmount * (double)100).ToString());
                else
                    listViewItem.SubItems.Add("-");

                listViewItem.SubItems.Add(selectedGuarantor.Description);

                _totalGuarantorAmount += selectedGuarantor.Amount;
                totalGuarantorAmountPercent += selectedGuarantor.Amount / pAmount * (double)100;

                listViewGuarantors.Items.Add(listViewItem);
            }

            var totalItem = new ListViewItem("");
            totalItem.Font = listViewGuarantors.Font;
            totalItem.Font = new Font(totalItem.Font, FontStyle.Bold);
            totalItem.SubItems.Add(_totalGuarantorAmount.GetFormatedValue(_credit.UseCents));
            totalItem.SubItems.Add(totalGuarantorAmountPercent.GetFormatedValue(true));
            listViewGuarantors.Items.Add(totalItem);
        }

        private void listViewGuarantors_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) { }

        private void listViewGuarantors_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) { }

        private void DisplayLoanEvents(Loan pCredit)
        {
            lvEvents.Items.Clear();
            List<Event> events = pCredit.Events.GetSortedEvents();
            LoanServices contractServices = ServicesProvider.GetInstance().GetContractServices();

            for (int i = events.Count - 1; i >= 0; i--)
            {
                Event e = events[i];
                if ((e is RepaymentEvent)
                    || (e is LoanDisbursmentEvent)
                    || (e is LoanValidationEvent)
                    || e is AccruedInterestEvent
                    || e is RescheduleLoanEvent
                    || e is TrancheEvent
                    || e is OverdueEvent
                    || e is ProvisionEvent
                    || e is LoanCloseEvent
                    || e is ManualScheduleChangeEvent
                    || e is LoanPenaltyAccrualEvent
                    || e is LoanInterestAccrualEvent
                    || e is LoanTransitionEvent)
                {
                    e.Cancelable = true;
                    if (e is LoanDisbursmentEvent)
                    {
                        if (((LoanDisbursmentEvent)e).Commissions != null)
                            foreach (var feeEvent in ((LoanDisbursmentEvent)e).Commissions)
                            {
                                feeEvent.Cancelable = true;
                            }
                    }
                    e.ExportedDate = contractServices.EventsExportedDate(e.Id, null);

                    if (!e.Cancelable) break;
                }
                else
                {
                    break;
                }
            }

            foreach (Event displayEvent in events)
            {
                var listViewItem = new ListViewItem(displayEvent.Date.ToShortDateString());
                listViewItem.SubItems.Add(displayEvent.EntryDate.ToShortDateString());

                listViewItem.SubItems.Add(displayEvent.Code);
                listViewItem.Tag = displayEvent;

                if (displayEvent is LoanDisbursmentEvent)
                {
                    var e = displayEvent as LoanDisbursmentEvent;
                    listViewItem.SubItems.Add(e.Amount.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(e.Fee.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is LoanEntryFeeEvent)
                {
                    LoanEntryFeeEvent e = displayEvent as LoanEntryFeeEvent;

                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(e.Fee.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is AccruedInterestEvent)
                {
                    AccruedInterestEvent evt = displayEvent as AccruedInterestEvent;
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(evt.AccruedInterest.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is RepaymentEvent)
                {
                    RepaymentEvent _event = displayEvent as RepaymentEvent;
                    listViewItem.SubItems.Add(_event.Principal.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Interests.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Commissions.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Penalties.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is BadLoanRepaymentEvent)
                {
                    BadLoanRepaymentEvent _event = displayEvent as BadLoanRepaymentEvent;
                    listViewItem.SubItems.Add(_event.Principal.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Interests.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Commissions.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Penalties.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is TrancheEvent)
                {
                    TrancheEvent _event = displayEvent as TrancheEvent;
                    listViewItem.SubItems.Add(_event.Amount.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is OverdueEvent)
                {
                    OverdueEvent _event = displayEvent as OverdueEvent;
                    listViewItem.SubItems.Add(_event.OLB.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(_event.OverduePrincipal.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.OverdueDays.ToString());
                }
                else if (displayEvent is ProvisionEvent)
                {
                    ProvisionEvent _event = displayEvent as ProvisionEvent;
                    listViewItem.SubItems.Add(_event.Amount.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is LoanPenaltyAccrualEvent)
                {
                    LoanPenaltyAccrualEvent _event = displayEvent as LoanPenaltyAccrualEvent;
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(_event.Penalty.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is LoanInterestAccrualEvent)
                {
                    LoanInterestAccrualEvent _event = displayEvent as LoanInterestAccrualEvent;
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(_event.Interest.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is LoanTransitionEvent)
                {
                    var _event = displayEvent as LoanTransitionEvent;
                    listViewItem.SubItems.Add(_event.Amount.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is RegEvent
                         || displayEvent is WriteOffEvent
                         || displayEvent is LoanValidationEvent
                         || displayEvent is LoanCloseEvent
                         || displayEvent is ManualScheduleChangeEvent)
                {
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is RescheduledLoanRepaymentEvent)
                {
                    RescheduledLoanRepaymentEvent _event = displayEvent as RescheduledLoanRepaymentEvent;
                    listViewItem.SubItems.Add(_event.Principal.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Interests.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Commissions.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add(_event.Penalties.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is RescheduleLoanEvent)
                {
                    RescheduleLoanEvent _event = displayEvent as RescheduleLoanEvent;
                    listViewItem.SubItems.Add(_event.Amount.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                else if (displayEvent is CreditInsuranceEvent)
                {
                    CreditInsuranceEvent _event = displayEvent as CreditInsuranceEvent;
                    listViewItem.SubItems.Add(_event.Principal.GetFormatedValue(pCredit.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add(_event.Commission.GetFormatedValue(pCredit.Product.Currency.UseCents));
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                    listViewItem.SubItems.Add("-");
                }
                
                listViewItem.SubItems.Add(displayEvent.Cancelable.ToString());
                listViewItem.SubItems.Add(displayEvent.User.ToString());

                if (displayEvent.ExportedDate.Date >= new DateTime(1900, 1, 1, 12, 0, 0))
                    listViewItem.SubItems.Add(displayEvent.ExportedDate.ToShortDateString());
                else
                    listViewItem.SubItems.Add("-");

                listViewItem.SubItems.Add(displayEvent.Id.ToString());
                listViewItem.SubItems.Add(displayEvent.InstallmentNumber.ToString());
                listViewItem.SubItems.Add(displayEvent.Comment);
                if (displayEvent.PaymentMethod != null)
                {
                    listViewItem.SubItems.Add(displayEvent.PaymentMethod.Name);
                }
                else
                {
                    listViewItem.SubItems.Add(string.Empty);
                }
                if (displayEvent.CancelDate.HasValue)
                    listViewItem.SubItems.Add(displayEvent.CancelDate.Value.ToString("dd/MM/yyyy HH:mm:ss"));
                else
                    listViewItem.SubItems.Add(string.Empty);

                if (displayEvent.Deleted)
                {
                    listViewItem.SubItems.Add(MultiLanguageStrings.GetString(Ressource.ClientForm, "Yes.Text"));
                    listViewItem.BackColor = Color.FromArgb(188, 209, 199);
                    listViewItem.ForeColor = Color.White;
                }
                else
                {
                    listViewItem.SubItems.Add(MultiLanguageStrings.GetString(Ressource.ClientForm, "No.Text"));
                }

                lvEvents.Items.Add(listViewItem);
            }
        }

        private void SelectAGuarantors()
        {
            var addGuarantor = new AddGuarantorForm(MdiParent, _credit.Product.Currency);
            addGuarantor.ShowDialog();

            if (!ServicesProvider.GetInstance().GetClientServices().GuarantorIsNull(addGuarantor.Guarantor))
            {
                if (_person != null)
                {
                    if (_person.Id != addGuarantor.Guarantor.Tiers.Id)
                    {
                        _listGuarantors.Add(addGuarantor.Guarantor);
                        DisplayGuarantors(_listGuarantors, _credit.Amount);
                        SaveContract();
                    }
                    else
                        MessageBox.Show(@"Person can not be a guarantor to himself/herself!", "Warning!",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    _listGuarantors.Add(addGuarantor.Guarantor);
                    DisplayGuarantors(_listGuarantors, _credit.Amount);
                    SaveContract();
                }
            }
        }

        private void buttonAddCollateral_Click(object sender, EventArgs e)
        {
            FillDropDownMenuWithCollateralProducts(menuCollateralProducts);
            menuCollateralProducts.Show(buttonAddCollateral, 0 - menuCollateralProducts.Size.Width, 0);
        }

        private void buttonSelectAGarantors_Click(object sender, EventArgs e)
        {
            SelectAGuarantors();
        }

        private void buttonDeleteEvent_Click(object sender, EventArgs e)
        {
            DeleteEvent();
        }

        private void DeleteEvent()
        {
            if (_credit.FundingLine == null && comboBoxLoanFundingLine.Tag == null)
            {
                MessageBox.Show(MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractIsReadOnly.Text"));
                return;
            }
            try
            {
                LoanServices cServices = ServicesProvider.GetInstance().GetContractServices();

                Event foundEvent = _credit.GetLastNonDeletedEvent();

                if (foundEvent == null)
                    throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventIsNull);

                if (!foundEvent.Cancelable)
                    throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.EventNotCancelable);

                var coaServices = ServicesProvider.GetInstance().GetChartOfAccountsServices();
                var fiscalYear = coaServices.SelectFiscalYears().Find(y => y.OpenDate <= foundEvent.Date && (y.CloseDate >= foundEvent.Date || y.CloseDate == null));
                if (null == fiscalYear || !fiscalYear.Open)
                {
                    throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.OperationOutsideCurrentFiscalYear);
                }

                List<Installment> archivedInstallments = cServices.GetArchivedInstallments(foundEvent.Id);
                // Request user confirmation
                var eventCancelConfirmationForm = new EventCancelConfirmationForm(_credit, foundEvent, archivedInstallments);
                DialogResult response = eventCancelConfirmationForm.ShowDialog();

                if (response == DialogResult.OK)
                {
                    IClient client = null;

                    if (_oClientType == OClientTypes.Corporate)
                        client = _corporateUserControl.Corporate;

                    if (_oClientType == OClientTypes.Person)
                        client = _personUserControl.Person;

                    if (_oClientType == OClientTypes.Group)
                        client = _groupUserControl.Group;

                    //foundEvent.Comment = eventCancelConfirmationForm.Comment;
                    //EventProcessorServices eps = ServicesProvider.GetInstance().GetEventProcessorServices();
                    //eps.UpdateCommentForLoanEvent(foundEvent, null);

                    Event _event = cServices.CancelLastEvent(_credit, client, eventCancelConfirmationForm.Comment);

                    //_credit.FundingLine.Remove(_event);

                    //update a loan for a client
                    foreach (Project prj in client.Projects)
                        foreach (Loan loan in prj.Credits)
                            if (loan.Code == _credit.Code)
                                loan.Disbursed = _credit.Disbursed;

                    if (_event is LoanDisbursmentEvent)
                    {
                        tabControlPerson.TabPages.Remove(tabPageLoanRepayment);
                        tabControlPerson.TabPages.Remove(tabPageSavingDetails);
                        tabControlPerson.SelectedTab = tabPageLoansDetails;

                        ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();

                        buttonLoanDisbursment.Enabled = true;
                        DisableCommitteeDecision(_credit.ContractStatus);

                        // If loan was disbursed to savings
                        if (_credit.CompulsorySavings != null)
                            SavingServices.DeleteLoanDisbursementSavingsEvent(_credit, _event);

                        // Update loan cycle
                        client.LoanCycle--;
                        if (client is Group)
                        {
                            foreach (Member member in (client as Group).Members)
                            {
                                member.Tiers.LoanCycle--;
                            }
                        }

                        if (client is Group)
                        {
                            _groupUserControl.SyncLoanCycle();
                        }
                        else if (client is Person)
                        {
                            _personUserControl.SyncLoanCycle();
                        }
                        else if (client is Corporate)
                        {
                            _corporateUserControl.SyncLoanCycle();
                        }
                    }
                    else
                    {
                        DisplayLoanEvents(_credit);
                        DisplayListViewLoanRepayments(_credit);
                    }

                    InitializeContractStatus(_credit);

                    if (_event is TrancheEvent)
                    {
                        InitializeTabPageLoanRepayment(_credit);
                        DisplayContracts(_project.Credits);
                    }

                    InitializeTabPageLoansDetails(_credit);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void buttonLoanReschedule_Click(object sender, EventArgs e)
        {
            try
            {
                if (_credit.Closed || _credit.ContractStatus.Equals(OContractStatus.Abandoned) ||
                    _credit.ContractStatus.Equals(OContractStatus.Refused))
                {
                    buttonLoanReschedule.Enabled = false;
                    MessageBox.Show("Cannot reschedule! Loan is closed, abandoned or refused.");
                }
                else
                {
                    if (_credit.FundingLine == null && comboBoxLoanFundingLine.Tag == null)
                    {
                        MessageBox.Show(MultiLanguageStrings.GetString(Ressource.ClientForm, "ContractIsReadOnly.Text"));
                        return;
                    }

                    var reschedulingForm = new ReschedulingForm(_credit, _client);
                    reschedulingForm.ShowDialog();

                    if (reschedulingForm.DialogResult != DialogResult.Cancel)
                    {
                        _credit = reschedulingForm.Contract;
                        InitializeContractStatus(_credit);
                        InitializeTabPageLoansDetails(_credit);
                        DisplayListViewLoanRepayments(_credit);
                        DisplayLoanEvents(_credit);
                        DisplayContracts(_project.Credits);
                    }
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void buttonModifyAGarantors_Click(object sender, EventArgs e)
        {
            if (listViewGuarantors.SelectedItems.Count != 0)
            {
                try
                {
                    AddGuarantorForm modifyGuarantor = new AddGuarantorForm((Guarantor)listViewGuarantors.SelectedItems[0].Tag, MdiParent, false, _credit.Product.Currency);
                    modifyGuarantor.ShowDialog();
                    if (!ServicesProvider.GetInstance().GetClientServices().GuarantorIsNull(modifyGuarantor.Guarantor))
                        DisplayGuarantors(_listGuarantors, _credit.Amount);

                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Please select proper item from the guarantor list!");
                }
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (listViewGuarantors.SelectedItems.Count != 0)
            {
                _listGuarantors.Remove((Guarantor)listViewGuarantors.SelectedItems[0].Tag);
                _credit.Guarantors.Remove((Guarantor)listViewGuarantors.SelectedItems[0].Tag);
                DisplayGuarantors(_listGuarantors, _credit.Amount);
                SaveContract();
            }
        }

        private void comboBoxLoanOfficer_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<User> users = ServicesProvider.GetInstance().GetUserServices().FindAll(true);

            List<User> myUser = users.FindAll(loanOfficer => loanOfficer.Name == cmbLoanOfficer.Text);

            myUser.ForEach(delegate(User loanOfficer)
            {
                _credit.LoanOfficer = loanOfficer;
            });
        }

        private void buttonProjectSelectPurpose_Click(object sender, EventArgs e)
        {
            FrmProjectPurposesR purpo = new FrmProjectPurposesR();
            purpo.ShowDialog();

            textBoxProjectPurpose.Clear();
            if (purpo.Purpose != null)
            {
                textBoxProjectPurpose.Text = purpo.Purpose;
            }
        }

        private void comboBoxLoanFundingLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(comboBoxLoanFundingLine.SelectedItem is FundingLine))
            {
                comboBoxLoanFundingLine.Tag = null;
                return;
            }
            _fundingLine = (FundingLine)comboBoxLoanFundingLine.SelectedItem;
            if (!_fundingLine.Deleted)
            {
                comboBoxLoanFundingLine.ForeColor = Color.Black;
                comboBoxLoanFundingLine.Font = new Font(Font, FontStyle.Regular);
                buttonLoanRepaymentRepay.Enabled = true;
                if (_credit != null && !_credit.WrittenOff)
                    btnWriteOff.Enabled = true;
            }
            comboBoxLoanFundingLine.Tag = _fundingLine;

        }

        private OContractStatus GetNewCreditStatus()
        {
            return ((KeyValuePair<OContractStatus, string>)cmbContractStatus.SelectedItem).Key;
        }

        private void buttonCreditCommiteeSaveDecision_Click(object sender, EventArgs e)
        {
            if (buttonCreditCommiteeSaveDecision.Text.Equals(GetString("update")))
            {
                EnableBoxCreditCommitteeComponents(true);
                buttonCreditCommiteeSaveDecision.Enabled = true;
                buttonCreditCommiteeSaveDecision.Text = GetString("save");
            }
            else
            {
                OContractStatus currentCreditStatus = OContractStatus.Pending;
                OContractStatus currentGuaranteeStatus = OContractStatus.Pending;
                try
                {
                    if (_credit != null)
                    {
                        OContractStatus newStatus = GetNewStatusAndValidate(ref currentCreditStatus);

                        _credit.ContractStatus = newStatus;
                        _credit.CreditCommiteeComment = textBoxCreditCommiteeComment.Text;
                        _credit.CreditCommiteeDate = dateTimePickerCreditCommitee.Value;
                        _credit.CreditCommitteeCode = tBCreditCommitteeCode.Text;

                        IClient client = null;
                        if (_oClientType == OClientTypes.Corporate) client = _corporateUserControl.Corporate;
                        if (_oClientType == OClientTypes.Person) client = _personUserControl.Person;
                        if (_oClientType == OClientTypes.Group) client = _groupUserControl.Group;

                        if (OContractStatus.Refused == newStatus
                            || OContractStatus.Abandoned == newStatus
                            || OContractStatus.Closed == newStatus)
                        {
                            _credit.Closed = true;
                            if (client != null)
                            {
                                if (client.ActiveLoans != null)
                                {
                                    if (client.ActiveLoans.Count == 0)
                                    {
                                        client.Active = false;
                                        client.Status = OClientStatus.Inactive;
                                    }
                                }
                            }
                            // updating guarantor status
                            if (_listGuarantors != null && _listGuarantors.Count > 0)
                            {
                                foreach (Guarantor guarantor in _listGuarantors)
                                {
                                    guarantor.Tiers.Active = false;
                                    guarantor.Tiers.Status = OClientStatus.Inactive;
                                    ServicesProvider.GetInstance().GetClientServices().UpdateClientStatus(guarantor.Tiers);
                                }
                            }
                        }
                        else
                        {
                            _credit.Closed = false;
                            if (client != null)
                            {
                                client.Active = true;
                                client.Status = OClientStatus.Active;
                            }
                        }

                        _credit = ServicesProvider.GetInstance().GetContractServices().
                            UpdateContractStatus(_credit, _project, client, currentCreditStatus == OContractStatus.Validated);

                        DisableCommitteeDecision(OContractStatus.Validated);

                        if (_credit.ContractStatus != OContractStatus.Validated)
                        {
                            var pendingOrPostponed = _credit.PendingOrPostponed();
                            if (!pendingOrPostponed)
                                ServicesProvider.GetInstance().GetContractServices().UpdateTiersStatus(_credit, client);

                            SetGuarantorsEnabled(pendingOrPostponed);
                            InitializeTabPageGuaranteesDetailsButtons(_credit.Product.UseGuarantorCollateral);
                        }
                        else
                        {
                            btnEditSchedule.Enabled = false;
                            InitializeTabPageGuaranteesDetailsButtons(_credit.Product.UseGuarantorCollateral);
                            DisableContractDetails(OContractStatus.Validated);
                        }
                        InitLoanDetails(false, _credit.Disbursed, _credit.ContractStatus == OContractStatus.Validated);
                        tabPageLoansDetails.Enabled = true;

                        if (_credit.PendingOrPostponed())
                        {
                            // Enable loan details components
                            InitializeAmount(_credit, false);
                            InitializePackageInterestRate(_credit, false);
                            InitializePackageGracePeriod(_credit.Product, false);
                            InitializePackageNumberOfInstallments(_credit, false);
                            cmbLoanOfficer.Enabled = true;
                            textBoxLoanPurpose.Enabled = true;
                            textBoxComments.Enabled = true;
                            comboBoxLoanFundingLine.Enabled = true;
                            // Advanced settings tab
                            EnableLocAmountTextBox(_credit);
                            EnableInsuranceTextBox(_credit);
                            groupBoxEntryFees.Enabled = true;
                            btnEditSchedule.Enabled = true;
                            InitializePackageAnticipatedTotalRepaymentsPenalties(_credit.Product, false);
                            InitializePackageAnticipatedPartialRepaymentsPenalties(_credit.Product, false);
                            InitializePackageNonRepaymentPenalties(_credit.Product, false);

                            if (_credit != null && _credit.Id > 0)
                                InitializeCustomizableFields(OCustomizableFieldEntities.Loan, _credit.Id, false);
                            else
                                InitializeCustomizableFields(OCustomizableFieldEntities.Loan, null, false);
                        }
                        InitializePackageLoanCompulsorySavings(_credit.Product, false);

                        DisplayContracts(_project.Credits);
                    }

                    ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();

                }
                catch (Exception ex)
                {
                    if (_credit != null) _credit.ContractStatus = currentCreditStatus;
                    if (_guarantee != null) _guarantee.ContractStatus = currentGuaranteeStatus;
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                }
            }
        }

        private OContractStatus GetNewStatusAndValidate(ref OContractStatus currentCreditStatus)
        {
            currentCreditStatus = _credit.ContractStatus;
            OContractStatus newStatus = GetNewCreditStatus();

            if (newStatus.Equals(_credit.ContractStatus))
                throw new OpenCbsContractSaveException(OpenCbsContractSaveExceptionEnum.StatusNotModified);


            if (newStatus.Equals(OContractStatus.Validated))
            {
                if (_credit.Product.UseCompulsorySavings)
                {
                    _credit.CompulsorySavings = SavingServices.GetSavingForLoan(_credit.Id, true);
                    if (_credit.CompulsorySavings == null)
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.NoCompulsorySavings);
                }
            }
            return newStatus;
        }

        private void SetSavingControlsState(bool state)
        {
            cmbCompulsorySaving.Enabled = state;
            lbCompulsorySavings.Enabled = state;
            lbCompulsorySavingsAmount.Enabled = state;
            lbCompAmountPercentMinMax.Enabled = state;
            if (_credit.CompulsorySavings == null)
                linkCompulsorySavings.Enabled = state;
        }

        private void EnableAndLoadSavingControls()
        {
            LoadClientSavings();
            linkCompulsorySavings.Visible = true;
        }

        private void DisableAndLoadSavingControls()
        {
            LoadClientSavings();
            lbCompAmountPercentMinMax.Visible = true;
            linkCompulsorySavings.Visible = true;
        }

        private void EnableInsuranceTextBox(Loan credit)
        {
            if (credit.PendingOrPostponed() || credit.ContractStatus == 0)
            {
                if (credit.Product.CreditInsuranceMin == credit.Product.CreditInsuranceMax)
                    tbInsurance.Enabled = false;
                else tbInsurance.Enabled = true;
            }
            else
                tbInsurance.Enabled = false;

        }

        private void InitializeTabPageGuaranteesDetailsButtons(bool UseGuarantorCollateral)
        {
            bool enabled = UseGuarantorCollateral;
            try
            {
                ServicesProvider.GetInstance().GetContractServices().ModifyGuarantorsCollaterals();
            }
            catch
            {
                enabled = false;
            }
            buttonSelectAGarantors.Enabled =
            buttonModifyAGarantors.Enabled =
            buttonDelete.Enabled =
            buttonAddCollateral.Enabled =
            buttonDelCollateral.Enabled =
            buttonModifyCollateral.Enabled = enabled;
        }

        private void ApplyRulesPendingEvent()
        {
            pendingFundingLineEvent = new FundingLineEvent
            {
                Code = string.Concat(OFundingLineEventTypes.Commitment.ToString(), "/", textBoxLoanContractCode.Text),
                Type = OFundingLineEventTypes.Commitment,
                FundingLine = ((FundingLine)comboBoxLoanFundingLine.SelectedItem),
                Movement = OBookingDirections.Debit,
                Amount = ServicesHelper.ConvertStringToDecimal(nudLoanAmount.Text, true)
            };

            if (pendingFundingLineEvent.FundingLine != null)
                ServicesProvider.GetInstance().GetFundingLinesServices().ApplyRulesAmountEventFundingLine(pendingFundingLineEvent);
        }

        private void buttonDelCollateral_Click(object sender, EventArgs e)
        {
            if (listViewCollaterals.SelectedItems.Count != 0)
            {
                if (MessageBox.Show(ML.GetString(Ressource.ClientForm, "DeleteCollateralFromLoan"),
                    ML.GetString(Ressource.ClientForm, "GroupEmptyWarning.Caption"), MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    _collaterals.Remove((ContractCollateral)listViewCollaterals.SelectedItems[0].Tag);
                    _credit.Collaterals.Remove((ContractCollateral)listViewCollaterals.SelectedItems[0].Tag);
                    DisplayCollateral();
                    SaveContract();
                }
            }
        }

        private void buttonProjectAddFollowUp_Click(object sender, EventArgs e)
        {
            ProjectFollowUp projectFollowUp = new ProjectFollowUp();
            projectFollowUp.ShowDialog();

            if (projectFollowUp.FollowUp != null)
            {
                FollowUp followUp = projectFollowUp.FollowUp;
                _followUpList.Add(followUp);
                DisplayFollowUps(_followUpList);
            }
        }

        private void DisplayFollowUps(List<FollowUp> pFollowUp)
        {
            listViewProjectFollowUp.Items.Clear();
            foreach (FollowUp up in pFollowUp)
            {
                ListViewItem item = new ListViewItem(up.Year.ToString());
                item.SubItems.Add(up.Jobs1.ToString());
                item.SubItems.Add(up.Jobs2.ToString());
                item.SubItems.Add(up.CA.GetFormatedValue(true));
                item.SubItems.Add(up.PersonalSituation);
                item.SubItems.Add(up.Activity);
                item.SubItems.Add(up.Comment);
                item.Tag = up;
                listViewProjectFollowUp.Items.Add(item);
            }
        }

        private void listViewProjectFollowUp_DoubleClick(object sender, EventArgs e)
        {
            FollowUp followUp = (FollowUp)listViewProjectFollowUp.SelectedItems[0].Tag;
            ProjectFollowUp projectFollowUp = new ProjectFollowUp(followUp);
            projectFollowUp.ShowDialog();
            DisplayFollowUps(_followUpList);
        }

        private void InitializeComboboxProjectJuridicStatus()
        {
            cBProjectJuridicStatus.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.LegalStatus);
            foreach (string s in list)
            {
                cBProjectJuridicStatus.Items.Add(s);
            }
        }

        private void InitializeComboboxProjectFiscalStatus()
        {
            cBProjectFiscalStatus.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.FiscalStatus);
            foreach (string s in list)
            {
                cBProjectFiscalStatus.Items.Add(s);
            }
        }

        private void InitializeComboboxProjectFinancialPlanType()
        {
            cBProjectFinancialPlanType.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.BusinessPlan);
            foreach (string s in list)
            {
                cBProjectFinancialPlanType.Items.Add(s);
            }
        }

        private void PersonUserControl_SavingSelected(object sender, EventArgs e)
        {
            InitializeTabPageSavingDetails((ISavingProduct)sender);
        }

        private void InitializeTabPageSavingDetails(ISavingProduct product)
        {
            try
            {
                Text = _title;
                _savingsBookProduct = (SavingsBookProduct)product;
                DisplaySavingProduct(product);

                tabControlSavingsDetails.TabPages.Clear();
                tabControlSavingsDetails.TabPages.Add(tabPageSavingsAmountsAndFees);
                tabControlSavingsDetails.TabPages.Add(tabPageSavingsEvents);
                tabControlSavingsDetails.TabPages.Add(tabPageLoans); ;
                tabControlSavingsDetails.TabPages.Add(tabPageSavingsCustomizableFields);
                _saving =
                    new SavingBookContract(ServicesProvider.GetInstance().GetGeneralSettings(),
                        User.CurrentUser,
                        (SavingsBookProduct)product);
                if (((SavingsBookProduct)product).UseTermDeposit) tabControlSavingsDetails.TabPages.Add(tpTermDeposit);

                groupBoxSaving.Text = string.Format("{0}",
                    MultiLanguageStrings.GetString(Ressource.ClientForm, "SavingsBook.Text"));
                groupBoxSaving.ForeColor = Color.FromArgb(0, 88, 56);

                tabControlPerson.TabPages.Remove(tabPageSavingDetails);
                tabControlPerson.TabPages.Add(tabPageSavingDetails);
                tabControlPerson.SelectedTab = tabPageSavingDetails;

                InitializeSavingsGeneralControls();
                InitializeTabPageTermDeposit();
                InitializeSavingsFees();

                btSavingsUpdate.Visible = false;

                groupBoxSaving.Enabled = true;
                pnlSavingsButtons.Enabled = false;

                groupBoxSaving.Name += string.Format(" {0}", product.Name);
                int numbersOfSavings = SavingServices.GetSavingCount(_client);
                _saving.GenerateSavingCode(_client, numbersOfSavings, ServicesProvider.GetInstance().GetGeneralSettings().SavingsCodeTemplate,
                    ServicesProvider.GetInstance().GetGeneralSettings().ImfCode, _client.Branch.Code);
                int nextSavingsId = SavingServices.GetLastSavingsId() + 1;
                tBSavingCode.Text = _saving.Code + '/' + nextSavingsId.ToString();

                InitializeSavingsOfficersComboBox();
                DisplaySavingEvent(_saving);
                DisplaySavingLoans(_saving);

                InitializeCustomizableFields(OCustomizableFieldEntities.Savings, null, false);
                LoadSavingsExtensions();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void InitializeTabPageTermDeposit()
        {
            cmbRollover2.DataSource = (from item in Enum.GetNames(typeof(OSavingsRollover))
                                       select new
                                                  {
                                                      Display = MultiLanguageStrings.GetString(
                                                      Ressource.ClientForm, "Rollover" + item + ".Text"),
                                                      Value = item
                                                  }).ToList();
            cmbRollover2.DisplayMember = "Display";
            cmbRollover2.ValueMember = "Value";
            if (_saving is SavingBookContract)
            {
                if (_saving.Id != 0)
                {
                    if (((SavingBookContract)_saving).UseTermDeposit)
                    {
                        if (((SavingBookContract)_saving).TermDepositPeriodMin != null &&
                            ((SavingBookContract)_saving).TermDepositPeriodMax != null &&
                            ((SavingBookContract)_saving).NumberOfPeriods != null)
                        {
                            nudNumberOfPeriods.Minimum = (decimal)((SavingBookContract)_saving).TermDepositPeriodMin;
                            nudNumberOfPeriods.Maximum = (decimal)((SavingBookContract)_saving).TermDepositPeriodMax;
                            nudNumberOfPeriods.Value = (decimal)((SavingBookContract)_saving).NumberOfPeriods;
                        }
                        cmbRollover2.SelectedValue = ((SavingBookContract)_saving).Rollover.ToString();
                        if (((SavingBookContract)_saving).TransferAccount != null)
                            tbTargetAccount2.Text = (((SavingBookContract)_saving).TransferAccount).Code;
                        tabControlSavingsDetails.TabPages.Remove(tpTermDeposit);
                        tabControlSavingsDetails.TabPages.Add(tpTermDeposit);
                    }
                    else
                    {
                        tabControlSavingsDetails.TabPages.Remove(tpTermDeposit);
                    }
                }
                else
                {
                    if (((SavingBookContract)_saving).UseTermDeposit)
                    {
                        if (((SavingBookContract)_saving).Product.TermDepositPeriodMin != null &&
                            ((SavingBookContract)_saving).Product.TermDepositPeriodMax != null)
                        {
                            nudNumberOfPeriods.Minimum = (decimal)((SavingBookContract)_saving).Product.TermDepositPeriodMin;
                            nudNumberOfPeriods.Maximum = (decimal)((SavingBookContract)_saving).Product.TermDepositPeriodMax;
                        }
                        tabControlSavingsDetails.TabPages.Remove(tpTermDeposit);
                        tabControlSavingsDetails.TabPages.Add(tpTermDeposit);
                    }
                    else
                    {
                        tabControlSavingsDetails.TabPages.Remove(tpTermDeposit);
                    }
                }
                lblLimitOfTermDepositPeriod.Text = string.Format("Min: {0}\nMax: {1}",
                                                                         nudNumberOfPeriods.Minimum,
                                                                         nudNumberOfPeriods.Maximum);
            }

        }

        private void InitializeSavingsGeneralControls()
        {
            buttonSaveSaving.Visible = true;
            buttonCloseSaving.Visible = false;
            buttonReopenSaving.Visible = false;
            nudDownInterestRate.Enabled = true;
            nudDownInitialAmount.Enabled = true;
            lbInterestRateMinMax.Visible = true;
        }

        private void InitializeSavingsFees()
        {
            nudWithdrawFees.Enabled = true;
            nudEntryFees.Enabled = true;
            nudTransferFees.Enabled = true;
            nudIbtFee.Enabled = true;
            nudDepositFees.Enabled = true;
            nudChequeDepositFees.Enabled = true;
            nudCloseFees.Enabled = true;
            nudManagementFees.Enabled = true;
            nudOverdraftFees.Enabled = true;
            nudAgioFees.Enabled = true;
            nudReopenFees.Enabled = true;
        }

        private void InitializeSavingsOfficersComboBox()
        {
            cmbSavingsOfficer.Items.Add(User.CurrentUser);
            foreach (User subordinate in User.CurrentUser.Subordinates)
            {
                if (!subordinate.IsDeleted && subordinate.UserRole.IsRoleForSaving)
                    cmbSavingsOfficer.Items.Add(subordinate);
            }
            cmbSavingsOfficer.SelectedIndex = 0;
        }

        private void DisplaySavingEvent(ISavingsContract pSaving)
        {
            btCancelLastSavingEvent.Enabled = false;

            if (pSaving.Id != 0)
            {
                nudDownInterestRate.Value = nudDownInterestRate.Minimum = nudDownInterestRate.Maximum = (decimal)pSaving.InterestRate * 100;
                nudDownInitialAmount.Value = nudDownInitialAmount.Minimum = nudDownInitialAmount.Maximum = pSaving.InitialAmount.Value;

                SavingBookContract s = (SavingBookContract)pSaving;
                nudEntryFees.Value = nudEntryFees.Minimum = nudEntryFees.Maximum = s.EntryFees.Value;

                nudWithdrawFees.Value = nudWithdrawFees.Minimum = nudWithdrawFees.Maximum = s.FlatWithdrawFees.HasValue ?
                    s.FlatWithdrawFees.Value : (decimal)s.RateWithdrawFees.Value * 100;

                nudTransferFees.Value = nudTransferFees.Minimum = nudTransferFees.Maximum = s.FlatTransferFees.HasValue ?
                    s.FlatTransferFees.Value : (decimal)s.RateTransferFees.Value * 100;

                nudIbtFee.Value = s.FlatInterBranchTransferFee.HasValue ? s.FlatInterBranchTransferFee.Value
                    : Convert.ToDecimal(s.RateInterBranchTransferFee.Value);
                nudIbtFee.Minimum = nudIbtFee.Maximum = nudIbtFee.Value;

                nudDepositFees.Value = nudDepositFees.Minimum = nudDepositFees.Maximum = ((SavingBookContract)pSaving).DepositFees.Value;
                nudChequeDepositFees.Value = nudChequeDepositFees.Minimum = nudChequeDepositFees.Maximum = ((SavingBookContract)pSaving).ChequeDepositFees.Value;
                nudCloseFees.Value = nudCloseFees.Minimum = nudCloseFees.Maximum = ((SavingBookContract)pSaving).CloseFees.Value;
                nudManagementFees.Value = nudManagementFees.Minimum = nudManagementFees.Maximum = ((SavingBookContract)pSaving).ManagementFees.Value;
                nudOverdraftFees.Value = nudOverdraftFees.Minimum = nudOverdraftFees.Maximum = ((SavingBookContract)pSaving).OverdraftFees.Value;
                nudAgioFees.Value = nudAgioFees.Minimum = nudAgioFees.Maximum = (decimal)((SavingBookContract)pSaving).AgioFees.Value * 100;
                nudReopenFees.Value = nudReopenFees.Minimum = nudReopenFees.Maximum = ((SavingBookContract)pSaving).ReopenFees.Value;

            }

            lbSavingBalanceValue.Text = pSaving.GetFmtBalance(true);
            lbSavingAvBalanceValue.Text = pSaving.GetFmtAvailBalance(true);
            btCancelLastSavingEvent.Enabled = _saving.HasCancelableEvents();

            lvSavingEvent.Items.Clear();
            IEnumerable<SavingEvent> events = pSaving.Events.OrderBy(item => item.Date.Date);

            bool useCents = pSaving.Product.Currency.UseCents;
            foreach (SavingEvent e in events)
            {
                ListViewItem item = new ListViewItem(e.Date.ToString("dd/MM/yyyy HH:mm:ss"));
                item.SubItems.Add(e.Fee.GetFormatedValue(useCents));
                string amt = e.Amount.GetFormatedValue(useCents);
                item.SubItems.Add(e.IsDebit ? amt : string.Empty);
                item.SubItems.Add(e.IsDebit ? string.Empty : amt);
                item.SubItems.Add(e.ExtraInfo);
                item.SubItems.Add(e.Code);
                item.SubItems.Add(e.SavingsMethod.HasValue ? GetString("SavingsOperationForm", e.SavingsMethod + ".Text") : "-");
                item.SubItems.Add(e.User.Name);
                item.SubItems.Add(e.Description);
                item.SubItems.Add(e.CancelDate.HasValue ? e.CancelDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty);

                if (e.IsPending)
                {
                    item.BackColor = Color.Orange;
                    item.ForeColor = Color.White;
                }

                if (e.Deleted)
                {
                    item.BackColor = Color.FromArgb(188, 209, 199);
                    item.ForeColor = Color.White;
                }

                item.Tag = e;
                lvSavingEvent.Items.Add(item);
            }
        }

        private void buttonFirstDeposit_Click(object sender, EventArgs e)
        {
            if (!CheckDataInOpenFiscalYear()) return;
            try
            {
                _savingsBookProduct = _saving.Product;
                var openSavingsForm = new OpenSavingsForm(_saving.InitialAmount, _saving.EntryFees, _savingsBookProduct, false);
                DialogResult result = openSavingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    SavingServices.FirstDeposit(_saving, openSavingsForm.InitialAmount, TimeProvider.Now, openSavingsForm.EntryFees,
                        User.CurrentUser, Teller.CurrentTeller);
                    SavingServices.UpdateInitialData(_saving.Id, openSavingsForm.InitialAmount, openSavingsForm.EntryFees);
                    _saving = SavingServices.GetSaving(_saving.Id);

                    for (int i = 0; i < _client.Savings.Count(); i++)
                    {
                        if (_client.Savings[i].Id == _saving.Id)
                        {
                            _client.Savings[i] = _saving;
                        }
                    }

                    tBSavingCode.Text = _saving.Code;
                    DisplaySaving(_saving);
                    DisplaySavings(_client.Savings);
                    DisplaySavingEvent(_saving);
                    pnlSavingsButtons.Enabled = true;
                    buttonFirstDeposit.Visible = false;
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void buttonSaveSaving_Click(object sender, EventArgs e)
        {
            try
            {
                _saving = new SavingBookContract(
                        ServicesProvider.GetInstance().GetGeneralSettings(),
                        User.CurrentUser,
                        TimeProvider.Now,
                        (SavingsBookProduct)_savingsBookProduct,
                        _person)
                {
                    InterestRate = Convert.ToDouble(nudDownInterestRate.Value) / 100,
                    InitialAmount = nudDownInitialAmount.Value
                };

                var savingBookContract = (SavingBookContract)_saving;
                var savingBookProduct = (SavingsBookProduct)_savingsBookProduct;

                savingBookContract.EntryFees = nudEntryFees.Value;

                if (savingBookProduct.WithdrawFeesType == OSavingsFeesType.Flat)
                    savingBookContract.FlatWithdrawFees = nudWithdrawFees.Value;
                else
                    savingBookContract.RateWithdrawFees = (double)nudWithdrawFees.Value / 100;

                if (savingBookProduct.TransferFeesType == OSavingsFeesType.Flat)
                    savingBookContract.FlatTransferFees = nudTransferFees.Value;
                else
                    savingBookContract.RateTransferFees = (double)nudTransferFees.Value / 100;

                if (savingBookProduct.InterBranchTransferFee.IsFlat)
                {
                    savingBookContract.FlatInterBranchTransferFee = nudIbtFee.Value;
                }
                else
                {
                    savingBookContract.RateInterBranchTransferFee = Convert.ToDouble(nudIbtFee.Value);
                }

                savingBookContract.DepositFees = nudDepositFees.Value;
                savingBookContract.ChequeDepositFees = nudChequeDepositFees.Value;
                savingBookContract.CloseFees = nudCloseFees.Value;
                savingBookContract.ManagementFees = nudManagementFees.Value;
                savingBookContract.OverdraftFees = nudOverdraftFees.Value;
                savingBookContract.AgioFees = (double)nudAgioFees.Value / 100;
                savingBookContract.ReopenFees = nudReopenFees.Value;
                if (savingBookContract.Product.UseTermDeposit)
                {
                    savingBookContract.UseTermDeposit = true;
                    savingBookContract.NumberOfPeriods = (int)nudNumberOfPeriods.Value;
                    savingBookContract.TermDepositPeriodMin = savingBookContract.Product.TermDepositPeriodMin;
                    savingBookContract.TermDepositPeriodMax = savingBookContract.Product.TermDepositPeriodMax;
                    savingBookContract.TransferAccount = SavingServices.GetSaving(tbTargetAccount2.Text);
                    savingBookContract.Rollover =
                        (OSavingsRollover)
                        Enum.Parse(typeof(OSavingsRollover), cmbRollover2.SelectedValue.ToString());
                }

                _customizableSavingsFieldsControl.Check();

                _saving.SavingsOfficer = (User)cmbSavingsOfficer.SelectedItem;
                _saving.Id = SavingServices.SaveContract(_saving, _client, (tx, id) =>
                {
                    foreach (var extension in SavingsExtensions) extension.Save(_saving, tx);
                });
                _saving = SavingServices.GetSaving(_saving.Id);

                if (_saving.Id > 0)
                    _customizableSavingsFieldsControl.Save(_saving.Id);

                _client.AddSaving(_saving);
                tBSavingCode.Text = _saving.Code;
                DisplaySaving(_saving);
                DisplaySavings(_client.Savings);
                DisplaySavingEvent(_saving);
                buttonSaveSaving.Visible = false;
                buttonFirstDeposit.Visible = true;
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void buttonSavingDeposit_Click(object sender, EventArgs e)
        {
            if (!CheckDataInOpenFiscalYear()) return;
            var savingEvent = new SavingsOperationForm(_saving, OSavingsOperation.Credit);
            savingEvent.ShowDialog();
            _saving = SavingServices.GetSaving(_saving.Id);
            DisplaySavingEvent(_saving);
            DisplaySavings(_client.Savings);
            ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();
        }

        private void buttonSavingWithDraw_Click(object sender, EventArgs e)
        {
            if (!CheckDataInOpenFiscalYear()) return;
            var savingsOperationForm = new SavingsOperationForm(_saving, OSavingsOperation.Debit);
            savingsOperationForm.ShowDialog();
            _saving = SavingServices.GetSaving(_saving.Id);
            DisplaySavingEvent(_saving);
            DisplaySavings(_client.Savings);
            ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();
        }

        private List<User> _subordinates;

        private void buttonModifyCollateral_Click(object sender, EventArgs e)
        {
            if (listViewCollaterals.SelectedItems.Count != 0)
            {
                try
                {
                    ContractCollateral contractCollateral = (ContractCollateral)listViewCollaterals.SelectedItems[0].Tag;
                    CollateralProduct collateralProduct = ServicesProvider.GetInstance().GetCollateralProductServices().
                        SelectCollateralProductByPropertyId(contractCollateral.PropertyValues[0].Property.Id);
                    CollateralProduct product = ServicesProvider.GetInstance().GetCollateralProductServices().SelectCollateralProduct(collateralProduct.Id);

                    ContractCollateralForm collateralForm = new ContractCollateralForm(product, contractCollateral, false);
                    collateralForm.ShowDialog();

                    if (collateralForm.ContractCollateral != null)
                    {
                        listViewCollaterals.SelectedItems[0].Tag = collateralForm.ContractCollateral;
                        _credit.Collaterals = _collaterals;
                        DisplayCollateral();
                        SaveContract();
                    }
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Please select proper item from the collateral list!");
                }
            }
        }

        private void btnLoanShares_Click(object sender, EventArgs e)
        {
            OCurrency oldAmount = null == _credit ? null : _credit.Amount;
            if (!oldAmount.HasValue)
            {
                oldAmount = ServicesHelper.ConvertStringToDecimal(nudLoanAmount.Text, null == _credit ? false : _credit.Product.UseCents);
            }
            _credit = Preview();
            if (null == _credit) return;
            if ((0 == _loanShares.Count || oldAmount != _credit.Amount) && _group.Members.Count > 0)
            {
                _loanShares.Clear();
                InitializeLoanShares();
            }
            _credit.LoanShares = _loanShares;
            LoanSharesForm frm = new LoanSharesForm(_credit, _group);
            if (DialogResult.OK != frm.ShowDialog()) return;
            if (_credit.Id > 0)
            {
                ServicesProvider.GetInstance().GetContractServices().UpdateLoanShares(_credit,
                                                                       _groupUserControl.Group.Id);
            }
        }

        private void InitializeLoanShares()
        {
            decimal num = _group.GetNumberOfMembers;
            if (!_credit.Amount.HasValue)
                _credit.Amount = nudLoanAmount.Value;
            decimal share = _credit.Amount.Value / num;
            decimal amt = _credit.Product.UseCents
                      ? Math.Round(share, 2, MidpointRounding.AwayFromZero)
                      : Math.Floor(share);
            OCurrency amtForLeader = _credit.Amount - (num - 1) * amt;
            _loanShares.Clear();

            Member leader = null;
            Member last = null;
            foreach (Member member in _group.Members)
            {
                leader = member.IsLeader ? member : null;
                last = member;
            }
            leader = leader ?? last;

            foreach (Member m in _group.Members)
            {
                LoanShare ls = new LoanShare
                {
                    Amount = m.Equals(leader) ? amtForLeader : amt
                    ,
                    PersonId = m.Tiers.Id
                    ,
                    PersonName = m.Tiers.Name
                };
                _loanShares.Add(ls);
            }
        }

        private void InitPrintButton(AttachmentPoint attachmentPoint, PrintButton button)
        {
            button.AttachmentPoint = attachmentPoint;
            Visibility visibility;
            switch (_oClientType)
            {
                case OClientTypes.Person:
                    visibility = Visibility.Individual;
                    break;

                case OClientTypes.Group:
                    visibility = Visibility.Group;
                    break;

                case OClientTypes.Corporate:
                    visibility = Visibility.Corporate;
                    break;

                default:
                    visibility = Visibility.All;
                    break;
            }
            button.Visibility = visibility;

            button.ReportInitializer =
                report =>
                {
                    report.SetParamValue("user_id", User.CurrentUser.Id);
                    if (_credit != null) report.SetParamValue("contract_id", _credit.Id);
                    if (_saving != null) report.SetParamValue("saving_id", _saving.Id);
                    if (_guarantee != null) report.SetParamValue("guarantee_id", _guarantee.Id);
                };
            button.LoadReports();
        }

        private void InitLoanDetailsPrintButton()
        {
            InitPrintButton(AttachmentPoint.LoanDetails, btnPrintLoanDetails);
        }

        private void InitLoanEventsPrintButton()
        {
            InitPrintButton(AttachmentPoint.LoanEvents, btnPrintLoanEvents);
        }

        private void InitLoanRepaymentPrintButton()
        {
            InitPrintButton(AttachmentPoint.LoanRepayment, btnPrintLoanRepayment);
        }

        private void InitCreditCommitteePrintButton()
        {
            InitPrintButton(AttachmentPoint.CreditCommittee, btnPrintCreditCommittee);
        }

        private void InitSavingsBookPrintButton()
        {
            InitPrintButton(AttachmentPoint.SavingsBook, btnPrintSavings);
        }

        private void InitGuarantorsPrintButton()
        {
            InitPrintButton(AttachmentPoint.Guarantors, btnPrintGuarantors);
        }
        private bool CheckDataInOpenFiscalYear()
        {
            try
            {
                var coaServices = ServicesProvider.GetInstance().GetChartOfAccountsServices();
                var fiscalYear =
                    coaServices.SelectFiscalYears().Find(y => y.OpenDate <= TimeProvider.Now && (y.CloseDate >= TimeProvider.Now || y.CloseDate == null));
                if (fiscalYear == null)
                {
                    throw new OpenCbsContractSaveException(
                        OpenCbsContractSaveExceptionEnum.OperationOutsideCurrentFiscalYear);
                }
                return true;
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                return false;
            }
        }

        private void btCancelLastSavingEvent_Click(object sender, EventArgs e)
        {
            try
            {

                if (!_saving.HasCancelableEvents()) return;

                EventStock es = new EventStock();
                es.AddRange(_saving.Events);
                Event foundEvent = es.GetLastSavingNonDeletedEvent;

                var coaServices = ServicesProvider.GetInstance().GetChartOfAccountsServices();
                var fiscalYear =
                    coaServices.SelectFiscalYears().Find(
                        y => y.OpenDate <= foundEvent.Date && (y.CloseDate >= foundEvent.Date || y.CloseDate == null));
                if (null == fiscalYear || !fiscalYear.Open)
                {
                    throw new OpenCbsContractSaveException(
                        OpenCbsContractSaveExceptionEnum.OperationOutsideCurrentFiscalYear);
                }

                string message = GetString("ConfirmCancelLastEvent");
                string caption = GetString("Confirm");
                DialogResult res = MessageBox.Show(message, caption, MessageBoxButtons.YesNo);
                if (res != DialogResult.Yes) return;

                FrmDeleteEventComment frm = new FrmDeleteEventComment();
                DialogResult result = frm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    try
                    {
                        if (_saving is SavingBookContract)
                            ((SavingBookContract)_saving).Loans = SavingServices.SelectLoansBySavingsId(_saving.Id);

                        SavingEvent sEvent = SavingServices.CancelLastEvent(_saving, User.CurrentUser, frm.Comment);

                        for (int i = 0; i <= _saving.Events.Count - 1; i++)
                            if (_saving.Events[i].Id == sEvent.Id)
                            {
                                SavingEvent temp = _saving.Events[i];
                                temp.Deleted = true;
                                _saving.Events[i] = temp;
                            }
                    }
                    catch (Exception ex)
                    {
                        new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                    }

                    ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();

                    DisplaySavingEvent(_saving);

                    if (_person != null) DisplaySavings(_person.Savings);
                    if (_client != null) DisplaySavings(_client.Savings);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void buttonCloseSaving_Click(object sender, EventArgs e)
        {
            if (!CheckDataInOpenFiscalYear()) return;
            foreach (SavingEvent savEvent in _saving.Events)
            {
                if (savEvent is SavingPendingDepositEvent && savEvent.IsPending && !savEvent.Deleted)
                {
                    var message = ML.GetString(Ressource.ClientForm, "SavingsAccountHasPendingEvents");
                    Fail(message);
                    return;
                }
            }

            // Compulsory savings
            if (_saving is SavingBookContract)
            {
                ((SavingBookContract)_saving).Loans = SavingServices.SelectLoansBySavingsId(_saving.Id);

                if (((SavingBookContract)_saving).Loans != null && ((SavingBookContract)_saving).Loans.Count > 0)
                {
                    foreach (Loan assosiatedLoan in ((SavingBookContract)_saving).Loans)
                    {
                        if (assosiatedLoan.ContractStatus == OContractStatus.Active)
                        {
                            var message = ML.GetString(Ressource.ClientForm, "SavingsAccountHasActiveLoans");
                            Fail(message);
                            return;
                        }
                    }
                }
            }

            OCurrency totalAmount = _saving.GetBalance(TimeProvider.Now);

            CloseSavingsForm closeSavingsForm;
            if (_saving is SavingBookContract)
            {
                closeSavingsForm = new CloseSavingsForm(_saving.Product, totalAmount, ((SavingBookContract)_saving).CloseFees);
            }
            else
            {
                closeSavingsForm = new CloseSavingsForm(totalAmount);
            }

            while (closeSavingsForm.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (closeSavingsForm.Amount < 0)
                    {
                        MessageBox.Show(@"Account with negative amount cannot be closed!");
                        return;
                    }

                    //((Saving)_saving).CloseFees = closeSavings.CloseFees;
                    if (_saving is SavingBookContract)
                    {
                        ((SavingBookContract)_saving).CloseFees = closeSavingsForm.CloseFees;
                    }

                    if (closeSavingsForm.IsWithdraw)
                    {
                        SavingServices.CloseAndWithdraw(_saving, TimeProvider.Now, User.CurrentUser,
                            closeSavingsForm.Amount, closeSavingsForm.IsDesactivateCloseFees, Teller.CurrentTeller);
                    }
                    else
                    {
                        SavingServices savingService = SavingServices;
                        ISavingsContract targetSaving = savingService.GetSaving(closeSavingsForm.ToSaving.Id);
                        savingService.CloseAndTransfer(_saving, targetSaving, TimeProvider.Now, User.CurrentUser, closeSavingsForm.Amount,
                            closeSavingsForm.IsDesactivateCloseFees, Teller.CurrentTeller);
                    }
                    _saving = SavingServices.GetSaving(_saving.Id);
                    DisplaySaving(_saving);
                    DisplaySavings(_client.Savings);
                    buttonReopenSaving.Visible = true;
                    break;
                }
                catch (Exception ex)
                {
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                }
            }
        }


        private void savingTransferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckDataInOpenFiscalYear()) return;
            var savingEvent = new SavingsOperationForm(_saving, OSavingsOperation.Transfer);
            savingEvent.ShowDialog();
            _saving = SavingServices.GetSaving(_saving.Id);
            DisplaySavingEvent(_saving);
            DisplaySavings(_client.Savings);
            ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();
        }

        private void OnFirstInstallmentDateChanged(object sender, EventArgs e)
        {
            _toChangeAlignDate = true;
            try
            {

                if (_oldFirstInstalmentDate.Date != dtpDateOfFirstInstallment.Value.Date &&
                    _oldFirstInstalmentDate != new DateTime(1, 1, 1) && !(_changeDisDateBool))
                {
                    //ServicesProvider.GetInstance().GetContractServices().ModifyFirstInstalmentDate(dateLoanStart.Value.Date);
                    _changeDisDateBool = false;
                }
                if (dateLoanStart.Value > dtpDateOfFirstInstallment.Value)
                {
                    dtpDateOfFirstInstallment.Value = dateLoanStart.Value;
                }

                if (_credit != null && _credit.Product != null && _toChangeAlignDate)
                {
                    _credit.AlignDisbursementDate = _credit.CalculateAlignDisbursementDate(dtpDateOfFirstInstallment.Value);
                    _firstInstallmentDate = dtpDateOfFirstInstallment.Value;
                }

                lblDay.Text = dtpDateOfFirstInstallment.Value.Date.DayOfWeek.ToString();
                _changeDisDateBool = false;
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                dtpDateOfFirstInstallment.Value = GetFirstInstallmentDate();
                throw;
            }

        }

        private DateTime GetFirstInstallmentDate()
        {
            DateTime date = dateLoanStart.Value;

            bool isSet = false;

            if (_credit.InstallmentType != null)
            {
                if (_credit.InstallmentType.NbOfDays > 0)
                {
                    date = date.AddDays(_credit.InstallmentType.NbOfDays);
                }

                if (_credit.InstallmentType.NbOfMonths > 0)
                {
                    date = date.AddMonths(_credit.InstallmentType.NbOfMonths);
                }

                isSet = true;
            }

            if (_product != null && !isSet)
            {
                if (_product.InstallmentType.NbOfDays > 0)
                {
                    date = date.AddDays(_product.InstallmentType.NbOfDays);
                }

                if (_product.InstallmentType.NbOfMonths > 0)
                {
                    date = date.AddMonths(_product.InstallmentType.NbOfMonths);
                }
            }

            if (_group != null)
            {
                if (_group.MeetingDay.HasValue)
                {
                    int delta = ServicesProvider.GetInstance().GetGeneralSettings().IsIncrementalDuringDayOff ? 1 : -1;
                    while (date.DayOfWeek != _group.MeetingDay)
                    {
                        date = date.AddDays(delta);
                    }

                    date = ServicesProvider.GetInstance().GetNonWorkingDate().GetTheNearestValidDate(date,
                                                                                                     ServicesProvider.
                                                                                                         GetInstance().
                                                                                                         GetGeneralSettings().
                                                                                                         IsIncrementalDuringDayOff,
                                                                                                     ServicesProvider.
                                                                                                         GetInstance().
                                                                                                         GetGeneralSettings().
                                                                                                         DoNotSkipNonWorkingDays,
                                                                                                     false);
                }
            }
            return date;
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            _toChangeAlignDate = true;
        }

        private void btSearchContract_Click(object sender, EventArgs e)
        {
            SearchCreditContractForm searchCreditContractForm = SearchCreditContractForm.GetInstance(Parent.Parent);
            searchCreditContractForm.BringToFront();
            searchCreditContractForm.WindowState = FormWindowState.Normal;

            string firstName = _person == null ? _client.Name : _person.FirstName;
            string lastName = _person == null ? _client.Name : _person.LastName;

            if (searchCreditContractForm.ShowForSearchSavingsContractForTransfer(string.Format("{0} {1}", firstName, lastName))
                == DialogResult.OK)
            {

                try
                {
                    ServicesProvider.GetInstance().GetSavingServices().CheckIfTransferAccountHasWrongCurrency(
                        _saving, searchCreditContractForm.SelectedSavingContract);
                    Button btn = sender as Button;
                    if (btn.Name.Equals(btSearchContract2.Name)) //The button is on the "term deposit" tab
                    {
                        tbTargetAccount2.Text = searchCreditContractForm.SelectedSavingContract.ContractCode;
                    }
                }
                catch (Exception ex)
                {
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                }


            }
        }



        private const int SavingNotSelectedValue = -1;

        private SavingBookContract GetSelectedSavingProduct()
        {
            if (cmbCompulsorySaving.SelectedItem == null) return null;

            var selectedValue = ((KeyValuePair<int, string>)cmbCompulsorySaving.SelectedItem).Key;
            if (selectedValue == SavingNotSelectedValue) return null;
            var savingsContract = SavingServices.GetSaving(selectedValue);
            Debug.Assert(savingsContract is SavingBookContract, "Compulsary savings should be savings book products");
            return (SavingBookContract)savingsContract;
        }

        private void linkCompulsorySavings_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Compulsory savings
            var savingContract = GetSelectedSavingProduct();
            if (savingContract == null)
            {
                MessageBox.Show(
                    MultiLanguageStrings.GetString(Ressource.ClientForm, "CompulsorySavingDetail.Text"),
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                return;
            }

            _saving = savingContract;
            DisplaySaving(_saving);
        }

        private void btSavingsUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                _customizableSavingsFieldsControl.Check();

                SavingServices.UpdateContract(_saving, _client);
                _saving = SavingServices.GetSaving(_saving.Id);

                if (_saving.Id > 0)
                    _customizableSavingsFieldsControl.Save(_saving.Id);

                DisplaySaving(_saving);
                DisplaySavings(_client.Savings);
                DisplaySavingEvent(_saving);
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void InitializeComboboxProjectAffiliation()
        {
            cBProjectAffiliation.Items.Clear();
            List<string> list = ServicesProvider.GetInstance().GetClientServices().FindAllSetUpFields(OSetUpFieldTypes.Registre);
            foreach (string s in list) cBProjectAffiliation.Items.Add(s);
        }

        private void textBoxLoanAnticipatedPartialFees_Leave(object sender, EventArgs e)
        {
            CheckAnticipatedFees(2);
        }

        private void buttonAddTranche_Click(object sender, EventArgs e)
        {
            try
            {
                ServicesProvider.GetInstance().GetContractServices().CheckLoanForTranche(_credit);

                var addTrancheForm = new AddTrancheForm(_credit, _client);
                addTrancheForm.ShowDialog();

                if (addTrancheForm.DialogResult != DialogResult.Cancel)
                {
                    _credit = addTrancheForm.Contract;
                    InitializeContractStatus(_credit);
                    InitializeTabPageLoansDetails(_credit);
                    DisplayListViewLoanRepayments(_credit);
                    DisplayLoanEvents(_credit);
                    DisplayContracts(_project.Credits);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void listViewLoansRepayments_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvLoansRepayments.ContextMenuStrip.Items.Remove(toolStripSeparatorCopy);
            lvLoansRepayments.ContextMenuStrip.Items.Remove(toolStripMenuItemEditComment);
            lvLoansRepayments.ContextMenuStrip.Items.Remove(toolStripMenuItemCancelPending);
            lvLoansRepayments.ContextMenuStrip.Items.Remove(toolStripMenuItemConfirmPending);

            if (lvLoansRepayments.SelectedItems.Count > 0)
            {
                var installment = lvLoansRepayments.SelectedItems[0].Tag as Installment;
                if (!installment.IsRepaid)
                {
                    lvLoansRepayments.ContextMenuStrip.Items.Add(toolStripSeparatorCopy);
                    lvLoansRepayments.ContextMenuStrip.Items.Add(toolStripMenuItemEditComment);
                }
                if (installment.IsPending)
                {
                    lvLoansRepayments.ContextMenuStrip.Items.Add(toolStripSeparatorCopy);
                    lvLoansRepayments.ContextMenuStrip.Items.Add(toolStripMenuItemConfirmPending);
                    lvLoansRepayments.ContextMenuStrip.Items.Add(toolStripMenuItemCancelPending);
                }
            }
        }

        private void toolStripMenuItemEditComment_Click(object sender, EventArgs e)
        {
            var installment = (Installment)lvLoansRepayments.SelectedItems[0].Tag;
            var editCommentDialog = new InstallmentCommentDialog() { Comment = installment.Comment };
            if (editCommentDialog.ShowDialog() == DialogResult.OK)
            {
                ServicesProvider.GetInstance().GetContractServices().UpdateInstallmentComment(editCommentDialog.Comment, _credit.Id, installment.Number);
                _credit.InstallmentList.FirstOrDefault(item => item.Number == installment.Number).Comment = editCommentDialog.Comment;
                DisplayListViewLoanRepayments(_credit);
            }
        }

        private void toolStripMenuItemCancelPending_Click(object sender, EventArgs e)
        {
            try
            {
                if (_credit.Events.GetLastLoanNonDeletedEvent is PendingRepaymentEvent)
                {
                    IClient client = null;
                    if (_oClientType == OClientTypes.Corporate) client = _corporateUserControl.Corporate;
                    if (_oClientType == OClientTypes.Person) client = _personUserControl.Person;
                    if (_oClientType == OClientTypes.Group) client = _groupUserControl.Group;

                    ServicesProvider.GetInstance().GetContractServices().CancelLastEvent(_credit, client, string.Empty);
                    DisplayLoanEvents(_credit);
                    DisplayListViewLoanRepayments(_credit);

                    InitializeContractStatus(_credit);
                    InitializeTabPageLoanRepayment(_credit);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void toolStripMenuItemConfirmPending_Click(object sender, EventArgs e)
        {
            try
            {
                IClient client = null;
                if (_oClientType == OClientTypes.Corporate) client = _corporateUserControl.Corporate;
                if (_oClientType == OClientTypes.Person) client = _personUserControl.Person;
                if (_oClientType == OClientTypes.Group) client = _groupUserControl.Group;

                _credit = ServicesProvider.GetInstance().GetContractServices().ConfirmPendingRepayment(_credit, client);
                DisplayListViewLoanRepayments(_credit);
                DisplayLoanEvents(_credit);
                InitializeContractStatus(_credit);
                if (_credit.Closed)
                {
                    buttonRepay.Enabled = false;
                    //we are sure that the last credit is closed, so we force here to make the last credit closed...
                    int count = _project.Credits.Count;
                    _project.Credits[count - 1].Closed = true;
                    DisplayContracts(_project.Credits);
                }

                if (_credit.InstallmentList[_credit.InstallmentList.Count - 1].IsRepaid)
                {
                    buttonLoanRepaymentRepay.Enabled = false;
                    buttonLoanReschedule.Enabled = false;
                    buttonReschedule.Enabled = false;
                    btnWriteOff.Enabled = false;
                }
            }

            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void MembersChanged(object sender, EventArgs e)
        {
            _loanShares.Clear();
        }

        private bool AreSharesUpToDate()
        {
            if (0 == _credit.Id) return false;

            List<LoanShare> shares = ServicesProvider.GetInstance().GetContractServices().GetLoanShares(_credit.Id);
            if (0 == shares.Count || 0 == _loanShares.Count) return false;
            if (shares.Count != _loanShares.Count) return false;

            foreach (LoanShare share in shares)
            {
                LoanShare compareTo = _loanShares.Find(x => x.PersonId == share.PersonId);
                if (null == compareTo) return false;
                if (share.Amount != compareTo.Amount) return false;
            }
            return true;
        }

        private void btnEditSchedule_Click(object sender, EventArgs e)
        {
            if (null == _credit || 0 == _credit.InstallmentList.Count) return;
            try
            {
                ServiceProvider.GetContractServices().ManualScheduleBeforeDisbursement();
                ManualScheduleForm manualScheduleForm = new ManualScheduleForm(_credit.Copy());

                if (manualScheduleForm.ShowDialog() == DialogResult.OK)
                {

                    _credit.ScheduleChangedManually = true;

                    if (_credit.ContractStatus != 0)
                        ServicesProvider.GetInstance().GetContractServices().SaveSchedule(
                            manualScheduleForm.Loan.InstallmentList, _credit);

                    _credit.InstallmentList = manualScheduleForm.Loan.InstallmentList;

                    DisplayInstallments(ref _credit);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }

        }

        private void textBoxLocAmount_Leave(object sender, EventArgs e)
        {
            tbLocAmount.BackColor = Color.White;
            btnSaveLoan.Enabled = true;
            btnUpdateSettings.Enabled = true;
            if (!string.IsNullOrEmpty(tbLocAmount.Text))
            {
                decimal locAmount = decimal.Parse(tbLocAmount.Text);
                if (locAmount < _product.AmountUnderLocMin.Value || locAmount > _product.AmountUnderLocMax.Value)
                {
                    tbLocAmount.BackColor = Color.Red;
                    btnSaveLoan.Enabled = false;
                    btnUpdateSettings.Enabled = false;
                    tbLocAmount.Focus();
                }
            }
            else
            {
                tbLocAmount.BackColor = Color.Red;
                btnSaveLoan.Enabled = false;
                btnUpdateSettings.Enabled = false;
                tbLocAmount.Focus();
            }
        }

        private void textBoxLocAmount_KeyPress(object sender, KeyPressEventArgs e)
        {
            int keyCode = e.KeyChar;

            if ((keyCode >= 48 && keyCode <= 57) || (keyCode == 8) || (Char.IsControl(e.KeyChar) && e.KeyChar
                != ((char)Keys.V | (char)Keys.ControlKey)) || (Char.IsControl(e.KeyChar) && e.KeyChar !=
                ((char)Keys.C | (char)Keys.ControlKey))
                || (e.KeyChar.ToString() == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
            {
                e.Handled = false;
            }
            else
                e.Handled = true;
        }

        private void textBoxLocAmount_TextChanged(object sender, EventArgs e)
        {
            if (tbLocAmount.Enabled)
            {
                tbLocAmount.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                if (!string.IsNullOrEmpty(tbLocAmount.Text))
                {
                    decimal locAmount = decimal.Parse(tbLocAmount.Text);
                    if (locAmount < _credit.Product.AmountUnderLocMin.Value
                        || locAmount > _credit.Product.AmountUnderLocMax.Value)
                    {
                        tbLocAmount.BackColor = Color.Red;
                        btnUpdateSettings.Enabled = false;
                    }
                }
                else
                {
                    tbLocAmount.BackColor = Color.Red;
                    btnUpdateSettings.Enabled = false;
                }
            }
        }

        private void textBoxLoanAnticipatedTotalFees_TextChanged(object sender, EventArgs e)
        {
            if (textBoxLoanAnticipatedTotalFees.Enabled)
            {
                textBoxLoanAnticipatedTotalFees.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                double anticipatedTotalFees =
                         ServicesHelper.ConvertStringToDouble(textBoxLoanAnticipatedTotalFees.Text, true);

                if (
                !ServicesHelper.CheckIfValueBetweenMinAndMax(_anticipatedTotalFeesValueRange.Min,
                                                                     _anticipatedTotalFeesValueRange.Max,
                                                                     anticipatedTotalFees))
                {
                    btnUpdateSettings.Enabled = false;
                    textBoxLoanAnticipatedTotalFees.BackColor = Color.Red;
                }
            }
        }

        private void textBoxLoanAnticipatedPartialFees_TextChanged(object sender, EventArgs e)
        {
            if (tbLoanAnticipatedPartialFees.Enabled)
            {
                tbLoanAnticipatedPartialFees.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;

                double anticipatedPartialFees =
                    ServicesHelper.ConvertStringToDouble(tbLoanAnticipatedPartialFees.Text, true);

                if (
                    !ServicesHelper.CheckIfValueBetweenMinAndMax(_anticipatedPartialFeesValueRange.Min,
                                                                 _anticipatedPartialFeesValueRange.Max,
                                                                 anticipatedPartialFees))
                {
                    tbLoanAnticipatedPartialFees.BackColor = Color.Red;
                    btnUpdateSettings.Enabled = false;
                }
            }
        }

        private void textBoxLoanLateFeesOnAmount_TextChanged(object sender, EventArgs e)
        {
            if (textBoxLoanLateFeesOnAmount.Enabled)
            {
                textBoxLoanLateFeesOnAmount.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnAmount.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnAmountRangeValue.Min,
                    _lateFeesOnAmountRangeValue.Max, lateFees))
                {
                    btnUpdateSettings.Enabled = false;
                    textBoxLoanLateFeesOnAmount.BackColor = Color.Red;
                }
            }
        }

        private void textBoxLoanLateFeesOnOverduePrincipal_TextChanged(object sender, EventArgs e)
        {
            if (textBoxLoanLateFeesOnOverduePrincipal.Enabled)
            {
                textBoxLoanLateFeesOnOverduePrincipal.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnOverduePrincipal.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnOverduePrincipalRangeValue.Min, _lateFeesOnOverduePrincipalRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnOverduePrincipal.BackColor = Color.Red;
                    btnUpdateSettings.Enabled = false;
                }
            }
        }

        private void textBoxLoanLateFeesOnOLB_TextChanged(object sender, EventArgs e)
        {
            if (textBoxLoanLateFeesOnOLB.Enabled)
            {
                textBoxLoanLateFeesOnOLB.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                double lateFees = ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnOLB.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnOLBRangeValue.Min, _lateFeesOnOLBRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnOLB.BackColor = Color.Red;
                    btnUpdateSettings.Enabled = false;
                }
            }
        }

        private void textBoxLoanLateFeesOnOverdueInterest_TextChanged(object sender, EventArgs e)
        {
            if (textBoxLoanLateFeesOnOverdueInterest.Enabled)
            {
                textBoxLoanLateFeesOnOverdueInterest.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                double lateFees =
                    ServicesHelper.ConvertStringToDouble(textBoxLoanLateFeesOnOverdueInterest.Text, true);
                if (!ServicesHelper.CheckIfValueBetweenMinAndMax(_lateFeesOnOverdueInterestRangeValue.Min, _lateFeesOnOverdueInterestRangeValue.Max, lateFees))
                {
                    textBoxLoanLateFeesOnOverdueInterest.BackColor = Color.Red;
                    btnUpdateSettings.Enabled = false;
                }
            }
        }

        private void tabControlPerson_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedTab = tabControlPerson.SelectedTab;
            if (selectedTab == tabPageCreditCommitee)
            {
                Control.ControlCollection controlCollection = gbxLoanDetails.Controls;
                bool isRedirect;

                CheckAndRedirect(controlCollection, tabPageLoansDetails, out isRedirect);
                if (isRedirect) return;

                controlCollection = groupBoxAnticipatedRepaymentPenalties.Controls;
                CheckAndRedirect(controlCollection, tabPageAdvancedSettings, out isRedirect);
                if (isRedirect) return;

                controlCollection = groupBoxLoanLateFees.Controls;
                CheckAndRedirect(controlCollection, tabPageAdvancedSettings, out isRedirect);
            }
        }

        private void LoadClientSavings()
        {
            var savingService = SavingServices;
            //Debug.Assert(_product != null, "Saving are loaded only if loan is initialized, if loan exist then product should too");
            if (_product == null) _product = _credit.Product;
            var clientSavings = savingService.SelectClientSavingBookCodes(_client.Id, _product.Currency.Id);

            var emptyItem = new KeyValuePair<int, string>(SavingNotSelectedValue, GetString("CompulsorySavingNotSelected.Text"));

            var items = cmbCompulsorySaving.Items;
            items.Clear();
            items.Add(emptyItem);
            foreach (var clientSaving in clientSavings)
            {
                items.Add(clientSaving);
                var creditSaving = _credit.CompulsorySavings;
                if (creditSaving != null && clientSaving.Key == creditSaving.Id)
                    cmbCompulsorySaving.SelectedItem = clientSaving;
            }

            if (cmbCompulsorySaving.SelectedItem == null)
                cmbCompulsorySaving.SelectedItem = emptyItem;
        }

        private void CheckAndRedirect(Control.ControlCollection controlCollection, TabPage tabPage, out bool isRedirect)
        {
            foreach (Control control in controlCollection)
            {
                if (control is TextBox || control is NumericUpDown)
                {
                    if (control.BackColor == Color.Red)
                    {
                        tabControlPerson.SelectTab(tabPage);
                        isRedirect = true;
                        return;
                    }
                }
            }
            isRedirect = false;
        }

        private void FillDropDownMenuWithCollateralProducts(ToolStrip pContextMenu)
        {
            pContextMenu.Items.Clear();
            CollateralProductServices productServices = ServicesProvider.GetInstance().GetCollateralProductServices();
            List<CollateralProduct> list = productServices.SelectAllCollateralProducts(false);
            foreach (CollateralProduct product in list)
            {
                var item = new ToolStripMenuItem(product.Name) { Tag = product.Id };
                item.Click += menuCollateralProductsItem_Click;
                pContextMenu.Items.Add(item);
            }
        }

        private void menuCollateralProductsItem_Click(object sender, EventArgs e)
        {
            var productId = (int)((ToolStripMenuItem)sender).Tag;
            CollateralProduct product = ServicesProvider.GetInstance().GetCollateralProductServices().SelectCollateralProduct(productId);

            ContractCollateralForm collateralForm = new ContractCollateralForm(product);
            collateralForm.ShowDialog();

            if (collateralForm.ContractCollateral != null && collateralForm.ContractCollateral.PropertyValues != null)
            {
                _collaterals.Add(collateralForm.ContractCollateral);
                _credit.Collaterals = _collaterals;
                DisplayCollateral();
                SaveContract();
            }
        }

        private void DisplayCollateral()
        {
            _totalCollateralAmount = 0;
            OCurrency totalCollateralAmountPercent = 0;
            listViewCollaterals.Items.Clear();

            foreach (ContractCollateral selectedCollateral in _collaterals)
            {
                CollateralProduct collateralProduct =
                    ServicesProvider.GetInstance().GetCollateralProductServices().SelectCollateralProductByPropertyId(selectedCollateral.PropertyValues[0].Property.Id);

                OCurrency selectedCollateralAmount = 0;
                foreach (CollateralPropertyValue propertyValue in selectedCollateral.PropertyValues)
                    if (propertyValue.Property.Name.Equals(GetString("FrmAddCollateralProduct", "propertyAmount")) ||
                        propertyValue.Property.Name.Equals("Montant") || propertyValue.Property.Name.Equals("Сумма") || propertyValue.Property.Name.Equals("Amount"))
                            selectedCollateralAmount = new OCurrency(Converter.CustomFieldValueToDecimal(propertyValue.Value));

                string selectedCollateralDescription = string.Empty;
                foreach (CollateralPropertyValue propertyValue in selectedCollateral.PropertyValues)
                    if (propertyValue.Property.Name.Equals(GetString("FrmAddCollateralProduct", "propertyDescription")) ||
                        propertyValue.Property.Name.Equals("Description") || propertyValue.Property.Name.Equals("Описание")) 
                        selectedCollateralDescription = propertyValue.Value;

                var listViewItem = new ListViewItem(collateralProduct.Name) { Tag = selectedCollateral };
                listViewItem.SubItems.Add(selectedCollateralAmount.GetFormatedValue(_credit.UseCents));
                if (_credit.Amount.HasValue)
                    listViewItem.SubItems.Add((selectedCollateralAmount / _credit.Amount * (double)100).ToString());
                else
                    listViewItem.SubItems.Add("-");
                listViewItem.SubItems.Add(selectedCollateralDescription);

                _totalCollateralAmount += selectedCollateralAmount;
                totalCollateralAmountPercent += selectedCollateralAmount / _credit.Amount * (double)100;

                listViewCollaterals.Items.Add(listViewItem);
            }

            var totalItem = new ListViewItem("");
            totalItem.Font = listViewCollaterals.Font;
            totalItem.Font = new Font(totalItem.Font, FontStyle.Bold);
            totalItem.SubItems.Add(_totalCollateralAmount.GetFormatedValue(_credit.UseCents));
            totalItem.SubItems.Add(totalCollateralAmountPercent.GetFormatedValue(true));
            listViewCollaterals.Items.Add(totalItem);
        }

        private void menuItemCancelPendingSavingEvent_Click(object sender, EventArgs e)
        {
            //SavingEvent savEvent = _saving.Events.Where(item => !item.Deleted).Last();
            var savEvent = lvSavingEvent.SelectedItems[0].Tag as SavingEvent;
            if (savEvent == null) throw new NullReferenceException();

            try
            {
                if (savEvent is SavingPendingDepositEvent)
                {
                    string message = GetString("ConfirmCancelLastEvent");
                    string caption = GetString("Confirm");

                    DialogResult res = MessageBox.Show(message, caption, MessageBoxButtons.YesNo);
                    if (res != DialogResult.Yes) return;

                    SavingServices.RefusePendingDeposit(savEvent.Amount, _saving,
                        savEvent.Date, savEvent.User, (OSavingsMethods)savEvent.SavingsMethod, savEvent.Id);

                    SavingServices.ChangePendingEventStatus(savEvent.Id, false);
                    savEvent.IsPending = false;
                    lvSavingEvent.SelectedItems[0].Tag = savEvent;

                    ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();

                    DisplaySavingEvent(_saving);
                    if (_person != null) DisplaySavings(_person.Savings);
                    if (_client != null) DisplaySavings(_client.Savings);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void menuItemConfirmPendingSavingEvent_Click(object sender, EventArgs e)
        {
            try
            {
                var savEvent = lvSavingEvent.SelectedItems[0].Tag as SavingEvent;
                if (savEvent == null) throw new NullReferenceException();

                SavingServices.Deposit(_saving, TimeProvider.Now,
                    savEvent.Amount, savEvent.Description, User.CurrentUser, false, (OSavingsMethods)savEvent.SavingsMethod, savEvent.Id, Teller.CurrentTeller);

                SavingServices.ChangePendingEventStatus(savEvent.Id, false);
                savEvent.IsPending = false;
                lvSavingEvent.SelectedItems[0].Tag = savEvent;

                ((LotrasmicMainWindowForm)_mdiParent).ReloadAlertsSync();

                DisplaySavingEvent(_saving);
                if (_person != null) DisplaySavings(_person.Savings);
                if (_client != null) DisplaySavings(_client.Savings);
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void listViewSavingEvent_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (lvSavingEvent.SelectedItems.Count > 0)
                {
                    var savEvent = lvSavingEvent.SelectedItems[0].Tag as SavingEvent;
                    if (savEvent is SavingPendingDepositEvent && !savEvent.Deleted && savEvent.IsPending)
                        lvSavingEvent.ContextMenuStrip = menuPendingSavingEvents;
                    else
                        lvSavingEvent.ContextMenuStrip = null;
                }
            }
        }

        private void SetGuarantorsEnabled(bool enabled)
        {
            pnlGuarantorButtons.Enabled = enabled;
            pnlCollateralButtons.Enabled = enabled;
            btnPrintGuarantors.Enabled = enabled;
        }

        private void buttonReopenSaving_Click(object sender, EventArgs e)
        {
            if (_saving is SavingBookContract)
            {
                try
                {
                    ServicesProvider.GetInstance().GetAccountingServices().FindExchangeRate(TimeProvider.Now, _saving.Product.Currency);

                    var openSavingsForm = new OpenSavingsForm(_saving.Product.InitialAmountMin, nudReopenFees.Value, _saving.Product, true);
                    DialogResult result = openSavingsForm.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        _saving.ReopenFees = openSavingsForm.EntryFees;
                        SavingServices.Reopen(openSavingsForm.InitialAmount, _saving, TimeProvider.Now, User.CurrentUser, _client);

                        _saving = SavingServices.GetSaving(_saving.Id);
                        DisplaySaving(_saving);
                        DisplaySavings(_client.Savings);
                        buttonReopenSaving.Visible = false;
                        buttonCloseSaving.Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                    return;
                }
            }
            else
            {
                MessageBox.Show(@"Only SavingsBook accounts can be reopened!");
            }
        }

        private void WriteOff(int writeOffMethodId)
        {
            if (_credit != null)
            {
                try
                {
                    ServicesProvider.GetInstance().GetContractServices().WriteOff(_credit, TimeProvider.Now, writeOffMethodId);
                    btnWriteOff.Enabled = false;
                    DisplayLoanEvents(_credit);
                    InitializeContractStatus(_credit);
                    if (MdiParent != null)
                    {
                        ((LotrasmicMainWindowForm)MdiParent).ReloadAlertsSync();
                    }
                }
                catch (Exception ex)
                {
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                    return;
                }
            }
        }

        private void btnWriteOff_Click(object sender, EventArgs e)
        {
            //if (Confirm("ConfirmWriteOff.Text"))
            //    WriteOff();
            int writeOffMethodId;
            var dialogResult = new WriteOffOkCancelForm().ShowDialogWrappe(out writeOffMethodId);

            if (dialogResult != DialogResult.Cancel)
                WriteOff(writeOffMethodId);
        }


        private void lvEntryFees_SubItemClicked(object sender, SubItemEventArgs e)
        {
            decimal min, max;
            if (!(e.Item.Tag is LoanEntryFee)) return;
            if (((LoanEntryFee)e.Item.Tag).ProductEntryFee.Min != null && ((LoanEntryFee)e.Item.Tag).ProductEntryFee.Max != null)
            {
                min = (decimal)((LoanEntryFee)e.Item.Tag).ProductEntryFee.Min;
                max = (decimal)((LoanEntryFee)e.Item.Tag).ProductEntryFee.Max;
            }
            else
            {
                min = (decimal)((LoanEntryFee)e.Item.Tag).ProductEntryFee.Value;
                max = (decimal)((LoanEntryFee)e.Item.Tag).ProductEntryFee.Value;
            }

            numEntryFees.Minimum = min;
            numEntryFees.Maximum = max;

            numEntryFees.DecimalPlaces = 0;
            numEntryFees.Increment = 1;

            if (_credit.Product.Currency.UseCents || ((LoanEntryFee)e.Item.Tag).ProductEntryFee.IsRate)
            {
                numEntryFees.DecimalPlaces = 2;
                numEntryFees.Increment = (decimal)0.01;
            }

            if (1 == e.SubItem && e.Item.Index < _credit.LoanEntryFeesList.Count)
            {
                lvEntryFees.StartEditing(numEntryFees, e.Item, e.SubItem);
            }
        }

        private void lbCompAmountPercentMinMax_TextChanged(object sender, EventArgs e)
        {
            ((Label)sender).Visible = true;
        }

        private void lvEntryFees_Click(object sender, EventArgs e)
        {
            decimal min, max;
            ListViewItem item = lvEntryFees.SelectedItems[0];
            if (!(item.Tag is LoanEntryFee)) return;
            if (((LoanEntryFee)item.Tag).ProductEntryFee.Min != null && ((LoanEntryFee)item.Tag).ProductEntryFee.Max != null)
            {
                min = (decimal)((LoanEntryFee)item.Tag).ProductEntryFee.Min;
                max = (decimal)((LoanEntryFee)item.Tag).ProductEntryFee.Max;
            }
            else
            {
                min = (decimal)((LoanEntryFee)item.Tag).ProductEntryFee.Value;
                max = (decimal)((LoanEntryFee)item.Tag).ProductEntryFee.Value;
            }

            string symbol = ((LoanEntryFee)item.Tag).ProductEntryFee.IsRate ? "%" : _credit.Product.Currency.Code;

            if (((LoanEntryFee)item.Tag).ProductEntryFee.IsRate)
                lblMinMaxEntryFees.Text = string.Format("Min: {0}\n\rMax: {1}",
                                                    ((OCurrency)(min)).GetFormatedValue(true) + symbol,
                                                    ((OCurrency)(max)).GetFormatedValue(true) + symbol);
            else
                lblMinMaxEntryFees.Text = string.Format("Min: {0}\n\rMax: {1}",
                                                        ((OCurrency)(min)).GetFormatedValue(
                                                            _credit.Product.Currency.UseCents) + " " + symbol,
                                                        ((OCurrency)(max)).GetFormatedValue(
                                                            _credit.Product.Currency.UseCents) + " " + symbol);
            lblMinMaxEntryFees.Visible = true;
        }

        private void specialOperationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckDataInOpenFiscalYear()) return;
            var savingEvent = new SavingsOperationForm(_saving, OSavingsOperation.SpecialOperation);
            savingEvent.ShowDialog();
            _saving = SavingServices.GetSaving(_saving.Id);
            DisplaySavingEvent(_saving);
            DisplaySavings(_client.Savings);
            ((LotrasmicMainWindowForm)MdiParent).ReloadAlertsSync();
        }

        private void ShowTotalFeesInListView(ListViewItem item)
        {
            OCurrency total = 0;
            total = _credit.Amount.HasValue ? _credit.GetSumOfFees() : _credit.GetSumOfFees(nudLoanAmount.Value);
            item.SubItems[3].Text = total.GetFormatedValue(_credit.Product.Currency.UseCents) + @" " + _credit.Product.Currency.Code;
        }

        private void lvEntryFees_SubItemEndEditing(object sender, SubItemEndEditingEventArgs e)
        {
            _credit.LoanEntryFeesList.Clear();
            OCurrency loanAmount = nudLoanAmount.Value;
            OCurrency inputFee = decimal.Parse(numEntryFees.Value.ToString());
            foreach (ListViewItem item in lvEntryFees.Items)
            {
                if (item.Tag is LoanEntryFee)
                {
                    _credit.LoanEntryFeesList.Add((LoanEntryFee)item.Tag);
                    if (e.Item.Index == item.Index)
                    {
                        ((LoanEntryFee)item.Tag).FeeValue = inputFee.Value;
                        if (((LoanEntryFee)item.Tag).ProductEntryFee.IsRate)
                        {
                            OCurrency feeAmount = loanAmount * inputFee / 100;
                            item.SubItems[3].Text = feeAmount.GetFormatedValue(_credit.Product.Currency.UseCents);
                        }
                        else
                        {
                            OCurrency feeAmount = inputFee;
                            item.SubItems[3].Text = feeAmount.GetFormatedValue(_credit.Product.Currency.UseCents);
                        }
                    }
                }
                else if (item.Tag.Equals("TotalFees"))
                {
                    ShowTotalFeesInListView(item);
                }
            }
        }

        private void buttonViewCollateral_Click(object sender, EventArgs e)
        {
            if (listViewCollaterals.SelectedItems.Count != 0)
            {
                try
                {
                    ContractCollateral contractCollateral = (ContractCollateral)listViewCollaterals.SelectedItems[0].Tag;
                    CollateralProduct collateralProduct = ServicesProvider.GetInstance().GetCollateralProductServices().
                        SelectCollateralProductByPropertyId(contractCollateral.PropertyValues[0].Property.Id);
                    CollateralProduct product = ServicesProvider.GetInstance().GetCollateralProductServices().SelectCollateralProduct(collateralProduct.Id);

                    ContractCollateralForm collateralForm = new ContractCollateralForm(product, contractCollateral, true);
                    collateralForm.ShowDialog();
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show(@"Please select proper item from the collateral list!");
                }
            }
        }

        private void buttonViewAGarantors_Click(object sender, EventArgs e)
        {
            if (listViewGuarantors.SelectedItems.Count != 0)
            {
                try
                {
                    AddGuarantorForm modifyGuarantor = new AddGuarantorForm((Guarantor)listViewGuarantors.SelectedItems[0].Tag, MdiParent, true, _credit.Product.Currency);
                    modifyGuarantor.ShowDialog();
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show(@"Please select proper item from the guarantor list!");
                }
            }
        }

        private void nudLoanAmount_ValueChanged(object sender, EventArgs e)
        {
            OCurrency amount = 0;
            if (_credit != null)
            {
                if (nudLoanAmount.Enabled)
                {
                    btnSaveLoan.Enabled = true;
                    decimal loanAmount;
                    if (decimal.TryParse(nudLoanAmount.Text, out loanAmount))
                        amount = ServicesHelper.ConvertStringToDecimal(nudLoanAmount.Text, 0, _credit.Product.UseCents);
                    else
                    {
                        btnSaveLoan.Enabled = false;
                        return;
                    }
                }

                if (_credit.LoanEntryFeesList != null)
                {
                    _credit.LoanEntryFeesList.Clear();

                    foreach (ListViewItem item in lvEntryFees.Items)
                    {
                        if (item.Tag is LoanEntryFee)
                        {
                            _credit.LoanEntryFeesList.Add((LoanEntryFee)item.Tag);
                        }
                        else if (item.Tag.Equals("TotalFees"))
                            ShowTotalFeesInListView(item);
                    }

                    foreach (ListViewItem item in lvEntryFees.Items)
                    {
                        if (item.Tag is LoanEntryFee)
                        {
                            LoanEntryFee entryFee = (LoanEntryFee)item.Tag;
                            if (entryFee.ProductEntryFee.IsRate)
                            {
                                OCurrency feeAmount = amount * entryFee.FeeValue / 100;
                                item.SubItems[3].Text = feeAmount.GetFormatedValue(_credit.Product.Currency.UseCents);
                            }
                            else
                            {
                                OCurrency feeAmount = entryFee.FeeValue;
                                item.SubItems[3].Text = feeAmount.GetFormatedValue(_credit.Product.Currency.UseCents);
                            }
                        }
                    }
                }
            }
        }

        private void nudLoanAmount_EnabledChanged(object sender, EventArgs e)
        {
            if (sender is NumericUpDown)
            {
                NumericUpDown numericUpDown = (NumericUpDown)sender;
                if (numericUpDown.Enabled == false)
                    numericUpDown.BackColor = Color.FromName("Window");
            }
            else if (sender is TextBox)
            {
                TextBox tb = (TextBox)sender;
                if (tb.Enabled == false)
                    tb.BackColor = Color.FromName("Window");
            }
        }

        private void nudLoanAmount_Leave(object sender, EventArgs e)
        {
            CheckAmount();
            var nud = (NumericUpDown)sender;
            decimal number;
            if (!decimal.TryParse(nud.Text, out number)) nud.Focus();
            nud.Text = ServicesHelper.ConvertStringToDecimal(nud.Text, null == _credit ? false : _credit.Product.UseCents).ToString();
            if (_group != null) InitializeLoanShares();
        }

        private void tbInsurance_TextChanged(object sender, EventArgs e)
        {
            if (tbInsurance.Enabled)
            {
                tbInsurance.BackColor = Color.White;
                btnUpdateSettings.Enabled = true;
                if (!string.IsNullOrEmpty(tbInsurance.Text))
                {
                    decimal insurance = decimal.Parse(tbInsurance.Text);
                    if (insurance < _credit.Product.CreditInsuranceMin || insurance > _credit.Product.CreditInsuranceMax)
                    {
                        tbInsurance.BackColor = Color.Red;
                        btnUpdateSettings.Enabled = false;
                    }
                }
                else
                {
                    tbInsurance.BackColor = Color.Red;
                    btnUpdateSettings.Enabled = false;
                }
            }
        }

        private void tbInsurance_Leave(object sender, EventArgs e)
        {
            if (tbInsurance.BackColor == Color.Red)
                tbInsurance.Focus();
        }

        private void ClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            GC.Collect();
        }

        private void WaiveFee()
        {
            if (_credit != null)
            {
                if (Confirm("WaiveFee.Text"))
                {
                    try
                    {
                        IClient aClient = null;
                        if (_oClientType == OClientTypes.Corporate)
                            aClient = _corporateUserControl.Corporate;

                        if (_oClientType == OClientTypes.Person)
                            aClient = _personUserControl.Person;

                        if (_oClientType == OClientTypes.Group)
                            aClient = _groupUserControl.Group;

                        ServicesProvider.GetInstance().GetContractServices().WaiveFee(ref _credit, ref aClient);

                        DisplayLoanEvents(_credit);
                        DisplayListViewLoanRepayments(_credit);
                        InitializeContractStatus(_credit);
                        InitializeTabPageLoansDetails(_credit);
                    }
                    catch (Exception ex)
                    {
                        new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                    }
                }
            }
        }

        private void btnWaiveFee_Click(object sender, EventArgs e)
        {
            WaiveFee();
        }


        private void dateLoanStart_ValueChanged(object sender, EventArgs e)
        {
            _changeDisDateBool = true;
            dtpDateOfFirstInstallment.Value = GetFirstInstallmentDate();
            _oldFirstInstalmentDate = dtpDateOfFirstInstallment.Value;
            try
            {
                if (_oldDisbursmentDate.Date != dateLoanStart.Value.Date && _oldDisbursmentDate != new DateTime(1, 1, 1))
                    ServicesProvider.GetInstance().GetContractServices().ModifyDisbursementDate(
                        dateLoanStart.Value.Date);

                if (dateLoanStart.Value > dtpDateOfFirstInstallment.Value)
                {
                    dtpDateOfFirstInstallment.Value = dateLoanStart.Value;
                }

                if (_credit != null && _credit.Product != null && _toChangeAlignDate)
                {
                    _credit.AlignDisbursementDate = _credit.CalculateAlignDisbursementDate(dtpDateOfFirstInstallment.Value);
                    _firstInstallmentDate = dtpDateOfFirstInstallment.Value;
                }

                lblDay.Text = dtpDateOfFirstInstallment.Value.Date.DayOfWeek.ToString();
                _oldDisbursmentDate = dateLoanStart.Value.Date;
            }
            catch (Exception ex)
            {
                dateLoanStart.Value = _oldDisbursmentDate;
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                throw;
            }
        }

        private void cmbRollover2_SelectedIndexChanged(object sender, EventArgs e)
        {
            btSearchContract2.Enabled =
                    (OSavingsRollover)Enum.Parse(typeof(OSavingsRollover),
                        cmbRollover2.SelectedValue.ToString())
                    != OSavingsRollover.PrincipalAndInterests;
        }

        private void cmbCompulsorySaving_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isCsSelected = cmbCompulsorySaving.SelectedIndex > 0;

            decimal minimum = numCompulsoryAmountPercent.Minimum;
            decimal maximum = numCompulsoryAmountPercent.Maximum;
            numCompulsoryAmountPercent.Enabled = (minimum != maximum) || isCsSelected;
            if (!isCsSelected)
                numCompulsoryAmountPercent.Value = _product == null ? 0 : minimum;
        }

        private void lvContracts_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblLoanStatus.Visible = false;
        }

        private void LoadLoanDetailsExtensions()
        {
            // Remove existing extension tab pages
            for (var i = tclLoanDetails.TabPages.Count - 1; i >= 0; i--)
            {
                var page = tclLoanDetails.TabPages[i];
                if (page.Tag is bool && (bool)page.Tag) tclLoanDetails.TabPages.Remove(page);
            }
            for (var i = tabControlRepayments.TabPages.Count - 1; i >= 0; i--) 

            {
                var page = tabControlRepayments.TabPages[i];
                if (page.Tag is bool && (bool)page.Tag) tabControlRepayments.TabPages.Remove(page);
            }

            foreach (var extension in LoanExtensions)
            {
                var pages = extension.GetTabPages(_credit);
                if (pages != null)
                {
                    foreach (var page in pages) page.Tag = true; // mark as extension
                    tclLoanDetails.TabPages.AddRange(pages);
                }

                pages = extension.GetRepaymentTabPages(_credit);
                if (pages != null)
                {
                    foreach (var page in pages) page.Tag = true; // mark as extension
                    tabControlRepayments.TabPages.AddRange(pages);
                }
            }
        }


        private void LoadSavingsExtensions()
        {
            // Remove existing extension tab pages
            for (var i = tabControlSavingsDetails.TabPages.Count - 1; i >= 0; i--)
            {
                var page = tabControlSavingsDetails.TabPages[i];
                if (page.Tag is bool && (bool) page.Tag)
                {
                    tabControlSavingsDetails.TabPages.Remove(page);
                }
            }

            foreach (var extension in SavingsExtensions)
            {
                var pages = extension.GetTabPages(_saving);
                if (null == pages) continue;
                foreach (var page in pages) page.Tag = true; // mark as extension
                tabControlSavingsDetails.TabPages.AddRange(pages);
            }
        }

        private void LoadLoanDetailsButtons()
        {
            foreach (var loanDetailsButton in LoanDetailsButtons)
            {
                var button = loanDetailsButton.GetButton(_client, _credit, _guarantee, _saving);
                if (button == null) continue;
                // Remove button if exists
                var controls = loanDetailsButtonsPanel.Controls.Find(button.Name, false);
                if (controls.Any()) loanDetailsButtonsPanel.Controls.Remove(controls[0]);
                loanDetailsButtonsPanel.Controls.Add(button);
            }
        }

        private void buttonSavingsOperations_Click(object sender, EventArgs e)
        {
            var point = buttonSavingsOperations.PointToScreen(this.buttonSavingsOperations.Location);
            point.X -= 12;
            point.Y -= 23;
            menuBtnAddSavingOperation.Show(this, point);
        }

        private void buttonManualSchedule_Click(object sender, EventArgs e)
        {
            if (null == _credit || 0 == _credit.InstallmentList.Count) return;
            try
            {
                ServiceProvider.GetContractServices().ManualScheduleAfterDisbursement();
                ManualScheduleForm manualScheduleForm = new ManualScheduleForm(_credit.Copy());

                if (manualScheduleForm.ShowDialog() == DialogResult.OK)
                {


                    var manualScheduleChangeEvent = new ManualScheduleChangeEvent();
                    manualScheduleChangeEvent.User = User.CurrentUser;
                    manualScheduleChangeEvent.Date = DateTime.Today;
                    ServiceProvider.GetContractServices()
                                   .AddManualScheduleChangeEvent(_credit, manualScheduleChangeEvent);

                    _credit = manualScheduleForm.Loan;

                    SaveContract();
                    _credit = ServiceProvider.GetContractServices().SelectLoan(_credit.Id, true, true, true);
                    DisplayListViewLoanRepayments(_credit);
                    DisplayLoanEvents(_credit);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }

        }




    }
}
