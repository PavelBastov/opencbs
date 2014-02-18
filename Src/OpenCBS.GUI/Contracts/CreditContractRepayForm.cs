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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.Contracts.Loans.LoanRepayment;
using OpenCBS.CoreDomain.Contracts.Savings;
using OpenCBS.CoreDomain.Events;
using OpenCBS.CoreDomain.Events.Loan;
using OpenCBS.GUI.Accounting;
using OpenCBS.GUI.Clients;
using OpenCBS.MultiLanguageRessources;
using OpenCBS.Services;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Shared;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain;
using OpenCBS.Enums;
using OpenCBS.GUI.UserControl;
using OpenCBS.Shared.Settings;

namespace OpenCBS.GUI.Contracts
{
    /// <summary>
    /// Summary description for CreditContractRepayForm.
    /// </summary>
    public partial class CreditContractRepayForm : SweetBaseForm
    {
        private Loan _loan;
        private readonly IClient _client;
        private ExchangeRate _exchangeRate;
        private DateTime _date;
        private int _instalmentNumber;
        private string _penaltiesExplanation;
        private bool _disableFees, _disableInterests, _keepExpectedInstallment, _manualAmountWasEntered;
        private bool _doProportionPayment;
        private OCurrency _amount, _manualPenalties, _manualInterests, _manualCommission;
        private RepaymentEvent _repaymentEvent;
        private OCurrency _regradingAmount;
        private OCurrency _maximumAmount;
        private PaymentMethod _paymentMethod;
        private OCurrency _principal;
        private OCurrency _interest;
        private OCurrency _commission;
        private Member _escapedMember;

        public CreditContractRepayForm(Loan pLoan, IClient pClient)
        {
            InitializeComponent();
            _client = pClient;
            _loan = pLoan;
            SetUp();
            toolTip = new ToolTip();
        }

        private void SetUp()
        {
            InitializeGeneralValues();
            _lbLoanCodeValue.Text = _loan.Code;
            _lbClientNameValue.Text = _client.Name;
            _lbInstalmentNbValue.Text = _instalmentNumber.ToString();
            _manualAmountWasEntered = false;
            buttonSelectAGroupPerson.Visible = false;

            dtpRepaymentDate.Value = TimeProvider.Today;

            if (_loan.Product.KeepExpectedInstallment)
                _rbKeepInitialSchedule.Checked = true;
            else
                _rbKeepNotInitialSchedule.Checked = true;

            nudICAmount.Minimum = 0;
            nudICAmount.Maximum = decimal.MaxValue;
            nudICAmount.DecimalPlaces = _loan.UseCents ? 2 : 0;

            nudECAmount.Minimum = 0;
            nudECAmount.Maximum = decimal.MaxValue;
            nudECAmount.DecimalPlaces = _loan.UseCents ? 2 : 0;
            FillComboBoxPaymentMethods();
            SetExchangeRate();

            nudICAmount.Value = _loan.CalculateAmountToRepaySpecifiedInstallment(_instalmentNumber, TimeProvider.Today, _disableFees, 
                _manualPenalties, _manualCommission, _disableInterests, _manualInterests, _keepExpectedInstallment).Value;

            DisplayAmountLabel();
            SetAmount(nudICAmount, _loan.UseCents);

            nudECAmount.Maximum = (_maximumAmount / _exchangeRate.Rate).Value;
            nudICAmount.Maximum = _maximumAmount.Value;

            textBoxComment.Text = _loan.GetFirstUnpaidInstallment().Comment;

            if(_loan.Product.LoanType == OLoanTypes.Flat || _loan.Product.LoanType == OLoanTypes.DecliningFixedInstallments)
            {
                rbProportionPayment.Visible = false;
            }

            DisplayInstallmentsAndEvent();
        }

        private void FillComboBoxPaymentMethods()
        {
            List<PaymentMethod> paymentMethods =
                ServicesProvider.GetInstance().GetPaymentMethodServices().GetAllPaymentMethods();
            foreach (PaymentMethod method in paymentMethods)
            {
                cmbPaymentMethod.Items.Add(method);
            }
            cmbPaymentMethod.SelectedItem = paymentMethods[0];
        }

        private void SetAmount(NumericUpDown pTextBoxAmount, bool useCents)
        {
            nudECAmount.ValueChanged -= nudECAmount_ValueChanged;
            nudICAmount.ValueChanged -= nudICAmount_ValueChanged;

            if(_exchangeRate == null)
            {
                _amount = ServicesHelper.ConvertStringToDecimal(pTextBoxAmount.Value.ToString(), 0, useCents);
            }
            else
            {
                if(pTextBoxAmount == nudICAmount)
                {
                    _amount = ServicesHelper.ConvertStringToDecimal(pTextBoxAmount.Value.ToString(), 0, useCents);
                    nudECAmount.Maximum = (_maximumAmount/_exchangeRate.Rate).Value;
                    nudECAmount.DecimalPlaces = _loan.UseCents ? 2 : 0;
                    nudECAmount.Value = (_amount/_exchangeRate.Rate).Value;
                }
                else
                {
                    OCurrency tempAmount = pTextBoxAmount.Value;
                    nudECAmount.DecimalPlaces = _loan.UseCents ? 2 : 0;
                    nudICAmount.Value = (tempAmount * _exchangeRate.Rate).Value;
                    
                    if (tempAmount == Math.Round((_maximumAmount / _exchangeRate.Rate).Value, 2))
                        nudICAmount.Value = _maximumAmount.Value;

                    _amount = nudICAmount.Value;
                }
            }

            nudECAmount.ValueChanged += nudECAmount_ValueChanged;
            nudICAmount.ValueChanged += nudICAmount_ValueChanged;
        }

        private void InitializeGeneralValues()
        {
            _instalmentNumber = _loan.GetFirstUnpaidInstallment().Number;
            _penaltiesExplanation = string.Empty;
            _disableFees = false;
            _disableInterests = false;
            _keepExpectedInstallment = _loan.Product.KeepExpectedInstallment;
            _manualPenalties = 0;
            _manualCommission = 0;
            _manualInterests = 0;

            _date = TimeProvider.Today.Date;
            _amount = _loan.CalculateAmountToRepaySpecifiedInstallment(_instalmentNumber, TimeProvider.Now.Date,
                _disableFees, _manualPenalties, _manualCommission, _disableInterests, _manualInterests, _keepExpectedInstallment);

            if(_loan.Product.LoanType == OLoanTypes.DecliningFixedPrincipalWithRealInterest)
                gbTypeOfRepayment.Visible = false;
        }

        private void SetExchangeRate()
        {
            _exchangeRate = null;
            if (ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies().Count == 1)
            {
                _exchangeRate = new ExchangeRate
                                    {
                                        Currency = _loan.Product.Currency,
                                        Date = TimeProvider.Today,
                                        Rate = 1
                                    };
            }

            try
            {
                if (!ServicesProvider.GetInstance().GetExchangeRateServices().RateExistsForEachCurrency
                    (ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies(), _date.Date))
                {
                    _btAddExchangeRate.Enabled = panelEC.Enabled = true;
                    ExchangeRateForm _xrForm = new ExchangeRateForm(new DateTime(_date.Year, _date.Month, _date.Day), _loan.Product.Currency);
                    _xrForm.ShowDialog();
                }
                _exchangeRate = ServicesProvider.GetInstance().GetAccountingServices().FindExchangeRate(_date.Date, _loan.Product.Currency);

               
            }
            catch(Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
            finally
            {
                if (_exchangeRate != null)
                {
                     panelEC.Visible = ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies().Count > 1 && !_loan.Product.Currency.IsPivot;
                    _btAddExchangeRate.Visible = false;
                }
                else
                {
                    panelEC.Visible = false;
                   _btAddExchangeRate.Enabled = true;
                }
                DisplayAmountLabel();
                DisplayInstallmentsAndEvent();
                
                if(!_manualAmountWasEntered)
                    nudICAmount.Value = _loan.CalculateAmountToRepaySpecifiedInstallment(_instalmentNumber, _date.Date, _disableFees, _manualPenalties, 
                        _manualCommission, _disableInterests, _manualInterests, _keepExpectedInstallment).Value;

                SetAmount(nudICAmount, _loan.UseCents);
            }
        }

        private void DisplayAmountLabel()
        {
            _lbICName.Text = _loan.Product.Currency.Name;

            _maximumAmount = _loan.CalculateMaximumAmountAuthorizedToRepay(_instalmentNumber, _date.Date, _disableFees, _manualPenalties, _manualCommission,
                _disableInterests, _manualInterests, _keepExpectedInstallment);

            _regradingAmount = _loan.CalculateMaximumAmountToRegradingLoan(_instalmentNumber, _date.Date, _disableFees, _manualPenalties, _manualCommission,
                _disableInterests, _manualInterests, _keepExpectedInstallment);

            _lbICAmountMinMax.Text = string.Format("{0}0\r\n{1}{2}",
                                     MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "min.Text"),
                                     MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "max.Text"), _maximumAmount.GetFormatedValue(_loan.UseCents));

            nudICAmount.Maximum = _maximumAmount.Value;

            if (checkBoxTotalAmount.Checked && _disableFees)
            {
                nudICAmount.Value = _maximumAmount.Value;
            }

            if (_disableFees && ServicesHelper.ConvertStringToDecimal(nudICAmount.Text, 0, _loan.UseCents) > _maximumAmount)
            {
                nudICAmount.Text = _maximumAmount.GetFormatedValue(_loan.UseCents);
            }

            lblAmountToGoBackNormal.Text = string.Format("{0} {1}",
                MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "amountToGoNormal.Text"),_regradingAmount.GetFormatedValue(_loan.UseCents));
            
            if (_loan.Product.LoanType == OLoanTypes.DecliningFixedPrincipalWithRealInterest)
                nudICAmount.Value = _regradingAmount.Value;


            if (_exchangeRate != null && ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies().Count != 1)
            {
                _lbECName.Text = ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Name;
                _lbECAmountMinMax.Text = string.Format("{0}0\r\n{1}{2}",
                                     MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "min.Text"),
                                     MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "max.Text"),
                                     (_maximumAmount / _exchangeRate.Rate).GetFormatedValue(_loan.UseCents));

                nudECAmount.Maximum = (_maximumAmount / _exchangeRate.Rate).Value;

                _lbECAmountToGoBackNormal.Text = string.Format("{0} {1}",
                                         MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "amountToGoNormal.Text"),
                                         (_regradingAmount / _exchangeRate.Rate).GetFormatedValue(_loan.UseCents));
            }
        }

        private void _btAddExchangeRate_Click(object sender, EventArgs e)
        {
            SetExchangeRate();
        }
        
        public Loan Contract
        {
            get { return _loan; }
        }

        public Member EscapedMember
        {
            get { return _escapedMember; }
        }

        private void checkBoxFees_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                textBoxPenalties.Text = _repaymentEvent.Penalties.GetFormatedValue(_loan.UseCents);
                textBoxCommission.Text = _repaymentEvent.Commissions.GetFormatedValue(_loan.UseCents);

                DiseableAutomaticsStatus();
                DisplayAmountLabel();
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void checkBoxInterests_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                textBoxInterest.Text = _repaymentEvent.Interests.GetFormatedValue(_loan.UseCents);

                DiseableAutomaticsStatus();
                DisplayAmountLabel();
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();                                                                                                                     
            }
        }

        // why do I always mistype method names?
        private void DiseableAutomaticsStatus()
        {
            if (!_keepExpectedInstallment)
            {
                // Diseable "Diseable automatic calculation" for Interest & Penalties
                _disableInterests = checkBoxInterests.Checked;
                textBoxInterest.Visible = _disableInterests;
                textBoxInterest.Enabled = _disableInterests;

                _disableFees = checkBoxFees.Checked;
                if (_loan.EscapedMember != null)
                {
                    textBoxPenalties.Visible = false;
                    textBoxPenalties.Enabled = false;
                }
                else
                {
                    textBoxPenalties.Visible = _disableFees;
                    textBoxPenalties.Enabled = _disableFees;
                }
                textBoxCommission.Visible = _disableFees;
                textBoxCommission.Enabled = _disableFees;
            }
            else
            {
                _disableInterests = checkBoxInterests.Checked;
                textBoxInterest.Visible = _disableInterests;
                textBoxInterest.Enabled = _disableInterests;

                _disableFees = checkBoxFees.Checked;
                textBoxPenalties.Visible = _disableFees;
                textBoxPenalties.Enabled = _disableFees;
                textBoxCommission.Visible = _disableFees;
                textBoxCommission.Enabled = _disableFees;

                if(checkBoxTotalAmount.Checked)
                {
                    nudICAmount.Value = _loan.CalculateMaximumAmountAuthorizedToRepay(_instalmentNumber, 
                                                                                        _date,
                                                                                        _disableFees,
                                                                                        _manualPenalties,
                                                                                        _manualCommission,
                                                                                        _disableInterests,
                                                                                        _manualInterests,
                                                                                        _keepExpectedInstallment).Value;
                }
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            Event lastEvent = _loan.Events.GetLastLoanNonDeletedEvent;
            var comment = textBoxComment.Text;
            var pending = checkBoxPending.Visible && checkBoxPending.Checked;

            if (lastEvent.Date.Date == dtpRepaymentDate.Value.Date)
            {
                string message = MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "alreadyAnEventForThisDate.Text");
                if (lastEvent is RescheduleLoanEvent)
                    message = GetString("AlreadyAnRoleEventForThisDate.Text");
                if (lastEvent is LoanDisbursmentEvent || lastEvent is LoanValidationEvent)
                    message = GetString("AlreadyAnLodeEventForThisDate.Text");
                DialogResult res = MessageBox.Show(message, "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No) return;
            }

            _date = new DateTime(dtpRepaymentDate.Value.Year, dtpRepaymentDate.Value.Month, dtpRepaymentDate.Value.Day); 

            try
            {
                bool isNotPaid = true;

                if (_date.Date < DateTime.Today.Date)
                    ServicesProvider.GetInstance().GetContractServices().PerformBackDateOperations();
                else if (_date.Date > DateTime.Today.Date)
                    ServicesProvider.GetInstance().GetContractServices().PerformFutureDateOperations();

                if (ApplicationSettings.GetInstance(User.CurrentUser.Md5).UseMandatorySavingAccount)
                {
                    ISavingsContract saving;
                    if (!(from item in _client.Savings where item.Product.Code == "default" select item).Any())
                    {
                        saving =
                            ServicesProvider.GetInstance()
                                            .GetSavingServices()
                                            .AddAndActivateDefaultSavingAccount(_client);
                        _client.Savings.Add(saving);
                    }
                    else
                        saving = (from item in _client.Savings where item.Product.Code == "default" select item).First();

                    var savingsMethod =
                        (OSavingsMethods)
                        Enum.Parse(typeof (OSavingsMethods), (cmbPaymentMethod.SelectedIndex == 0) ? "Cash" : "Cheque");
                    var savingEvent = ServicesProvider
                        .GetInstance()
                        .GetSavingServices()
                        .Deposit(saving, TimeProvider.Today, _amount, " ", User.CurrentUser, false, savingsMethod, null,
                                 Teller.CurrentTeller).FirstOrDefault();

                    using (var sqlTransaction = DatabaseConnection.GetConnection().BeginTransaction())
                    {
                        ServicesProvider.GetInstance()
                                        .GetContractServices()
                                        .CallInterceptor(new Dictionary<string, object>
                                            {
                                                {"Loan", _loan},
                                                {"Event", savingEvent},
                                                {"SqlTransaction", sqlTransaction}
                                            });
                        sqlTransaction.Commit();
                    }
                    var repaymentConfiguration = new RepaymentConfiguration()
                    {
                        Client = _client,
                        Date = _date,
                        Saving = saving,
                        DisableFees = _disableFees,
                        DisableInterests = _disableInterests,
                        ManualCommission = _manualCommission,
                        ManualFeesAmount = _manualPenalties,
                        ManualInterestsAmount = _manualInterests,
                        Loan = _loan,
                        KeepExpectedInstallment = _keepExpectedInstallment,
                        ProportionPayment = _doProportionPayment,
                        IsPending = pending
                    };
                    _loan = ServicesProvider.GetInstance()
                                            .GetContractServices()
                                            .RepayLoanFromTransitAccount(repaymentConfiguration);
                }
                else
                {
                    if (_disableInterests || _disableFees || !_keepExpectedInstallment)
                    {
                        if (_disableInterests)
                        {
                            _loan = ServicesProvider.GetInstance().GetContractServices().ManualInterestCalculation(
                                _loan, _client, _instalmentNumber,
                                _date, _amount, _disableFees, _manualPenalties, _manualCommission, _disableInterests,
                                _manualInterests,
                                _keepExpectedInstallment,
                                _doProportionPayment,
                                _paymentMethod, comment, pending);
                            isNotPaid = false;
                        }
                        if (_disableFees && isNotPaid)
                        {
                            _loan = ServicesProvider.GetInstance().GetContractServices().ManualFeesCalculation(_loan,
                                                                                                               _client,
                                                                                                               _instalmentNumber,
                                                                                                               _date,
                                                                                                               _amount,
                                                                                                               _disableFees,
                                                                                                               _manualPenalties,
                                                                                                               _manualCommission,
                                                                                                               _disableInterests,
                                                                                                               _manualInterests,
                                                                                                               _keepExpectedInstallment,
                                                                                                               _doProportionPayment,
                                                                                                               _paymentMethod,
                                                                                                               comment,
                                                                                                               pending);
                            isNotPaid = false;
                        }
                        if (!_keepExpectedInstallment && isNotPaid)
                        {
                            _loan = ServicesProvider.GetInstance().GetContractServices().ChangeRepaymentType(_loan,
                                                                                                             _client,
                                                                                                             _instalmentNumber,
                                                                                                             _date,
                                                                                                             _amount,
                                                                                                             _disableFees,
                                                                                                             _manualPenalties,
                                                                                                             _manualCommission,
                                                                                                             _disableInterests,
                                                                                                             _manualInterests,
                                                                                                             _keepExpectedInstallment,
                                                                                                             _doProportionPayment,
                                                                                                             _paymentMethod,
                                                                                                             comment,
                                                                                                             pending);
                        }
                    }
                    else
                    {
                        _loan = ServicesProvider.GetInstance().GetContractServices().Repay(_loan,
                                                                                           _client,
                                                                                           _instalmentNumber,
                                                                                           _date,
                                                                                           _amount,
                                                                                           _disableFees,
                                                                                           _manualPenalties,
                                                                                           _manualCommission,
                                                                                           _disableInterests,
                                                                                           _manualInterests,
                                                                                           _keepExpectedInstallment,
                                                                                           _doProportionPayment,
                                                                                           _paymentMethod,
                                                                                           comment,
                                                                                           pending);
                    }
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                throw;
            }
        }

        private void textBoxPenalties_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _manualPenalties = ServicesHelper.ConvertStringToDecimal(textBoxPenalties.Text, _loan.UseCents);
                if(checkBoxTotalAmount.Checked)
                    _amount = _amount - _repaymentEvent.Penalties + _manualPenalties;

                DisplayAmountLabel();
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                textBoxPenalties.Text = @"0";
            }
        }

        private void textBoxInterests_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _manualInterests = ServicesHelper.ConvertStringToDecimal(textBoxInterest.Text, _loan.UseCents);
                if (checkBoxTotalAmount.Checked)
                    _amount = _amount - _repaymentEvent.Interests + _manualInterests;

                DisplayAmountLabel();
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                textBoxInterest.Text = "0";
            }
        }

        private void SetRepaymentExplanation(RepaymentEvent pEvent)
        {
            _penaltiesExplanation = string.Empty;
            if (pEvent.PastDueDays != 0)
            {
                var strPenaltiesCalculation = new StringBuilder();
                var strPenalties = new StringBuilder();
                string pastDueDays = pEvent.PastDueDays.ToString();

                if (ServicesProvider.GetInstance().GetGeneralSettings().LateDaysAfterAccrualCeases < pEvent.PastDueDays)
                {
                    pastDueDays = ServicesProvider.GetInstance().GetGeneralSettings().LateDaysAfterAccrualCeases.ToString();
                }

                if (_loan.NonRepaymentPenalties.OverDuePrincipal != 0)
                {
                    strPenalties.AppendFormat("{0} = ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "Penalty.Text"));
                    strPenalties.AppendFormat("{0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "amount.Text"));
                    strPenalties.AppendFormat(" % {0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm,
                                                                                    "OverDuePrincipall.Text"));
                    strPenalties.AppendFormat("{0}\n", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "Latedays.Text"));

                    strPenaltiesCalculation.AppendFormat("{0} = {1} * {2} * {3}", pEvent.Penalties.GetFormatedValue(_loan.UseCents), 
                                pEvent.Principal.GetFormatedValue(_loan.UseCents), _loan.NonRepaymentPenalties.OverDuePrincipal, pastDueDays);
                }

                if (_loan.NonRepaymentPenalties.OverDueInterest != 0)
                {
                    if (strPenalties.Length == 0)
                    {
                        strPenalties.AppendFormat("{0} = ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm,"Penalty.Text"));
                        strPenaltiesCalculation.Append(pEvent.Penalties.GetFormatedValue(_loan.UseCents) + " = ");
                    }
                    else
                        strPenaltiesCalculation.Append(" + ");

                    strPenalties.Append(" + ");
                    strPenalties.AppendFormat("{0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "amount.Text"));
                    strPenalties.AppendFormat(" % {0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm,
                                                                                    "OverDueInterest.Text"));
                    strPenalties.AppendFormat("{0}\n", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "Latedays.Text"));
                    strPenaltiesCalculation.AppendFormat("{0} * {1} * {2}", pEvent.Principal.GetFormatedValue(_loan.UseCents), 
                                        _loan.NonRepaymentPenalties.OverDueInterest, pastDueDays);
                }

                if (_loan.NonRepaymentPenalties.OLB != 0)
                {
                    if (strPenalties.Length == 0)
                    {
                        strPenalties.AppendFormat("{0} = ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm,"Penalty.Text"));
                        strPenaltiesCalculation.AppendFormat("{0} = ", pEvent.Penalties.GetFormatedValue(_loan.UseCents));
                    }
                    else
                        strPenaltiesCalculation.Append(" + ");

                    strPenalties.Append(" + ");
                    strPenalties.AppendFormat("{0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "amount.Text"));
                    strPenalties.Append(" % OLB * ");
                    strPenalties.AppendFormat("{0}\n", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "Latedays.Text"));
                    strPenaltiesCalculation.AppendFormat("{0} * {1} * {2}", pEvent.Principal.GetFormatedValue(_loan.UseCents), 
                                            _loan.NonRepaymentPenalties.OLB, pastDueDays);
                }

                if (_loan.NonRepaymentPenalties.InitialAmount != 0)
                {
                    if (strPenalties.Length == 0)
                    {
                        strPenalties.AppendFormat("{0} = ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm,"Penalty.Text"));
                        strPenaltiesCalculation.AppendFormat("{0} = ", pEvent.Penalties.GetFormatedValue(_loan.UseCents));
                    }
                    else
                        strPenaltiesCalculation.Append(" + ");

                    strPenalties.Append(" + ");
                    strPenalties.AppendFormat("{0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "amount.Text"));
                    strPenalties.AppendFormat(" % {0} * ", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm,
                                                                                    "InitialAmount.Text"));
                    strPenalties.AppendFormat("{0}\n", MultiLanguageStrings.GetString(Ressource.CreditContractRepayForm, "Latedays.Text"));
                    strPenaltiesCalculation.AppendFormat("{0} * {1} * {2}", pEvent.Principal.GetFormatedValue(_loan.UseCents), 
                                            _loan.NonRepaymentPenalties.InitialAmount, pastDueDays);
                }

                strPenalties.AppendLine("\n");
                strPenalties.AppendLine(strPenaltiesCalculation.ToString());
                _penaltiesExplanation = strPenalties.ToString();
            }
        }

        private void labelRepayFees_MouseHover(object sender, EventArgs e)
        {
            toolTip.SetToolTip(labelRepayFees, _penaltiesExplanation);
        }

        private void DisplayInstallmentsAndEvent()
        {
            try
            {
                var pending = checkBoxPending.Visible && checkBoxPending.Checked;
                bool isTotalRepayment = checkBoxTotalAmount.Checked;

                KeyValuePair<Loan, RepaymentEvent> result =
                    ServicesProvider.GetInstance().GetContractServices().ShowNewContract(_loan,
                                                                                         _instalmentNumber,
                                                                                         _date,
                                                                                         _amount,
                                                                                         _disableFees,
                                                                                         _manualPenalties,
                                                                                         _manualCommission,
                                                                                         _disableInterests,
                                                                                         _manualInterests,
                                                                                         _keepExpectedInstallment,
                                                                                         _doProportionPayment,
                                                                                         _paymentMethod,
                                                                                         pending,
                                                                                         isTotalRepayment);

                SetInstalments(result.Key.InstallmentList);

                if (result.Value != null)
                {
                    SetEvent(result.Value);
                    SetRepaymentExplanation(result.Value);
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }

        private void SetEvent(RepaymentEvent pEvent)
        {
            _repaymentEvent = pEvent;
            _lbICPrincipal.Text = string.Format("{0} {1}", pEvent.Principal.GetFormatedValue(_loan.UseCents), _loan.Product.Currency.Code);
            lbInterest.Text = string.Format("{0} {1}", pEvent.Interests.GetFormatedValue(_loan.UseCents), _loan.Product.Currency.Code);
            _lbICFees.Text = string.Format("{0} {1}", pEvent.Penalties.GetFormatedValue(_loan.UseCents), _loan.Product.Currency.Code);
            _lbICCommisions.Text = string.Format("{0} {1}", pEvent.Commissions.GetFormatedValue(_loan.UseCents), _loan.Product.Currency.Code);

             if (_client is Group)
            {
                buttonSelectAGroupPerson.Visible = _client is Group;
                bool activeButton = false;

                foreach (Installment installment in _loan.InstallmentList)
                {
                    if (!installment.IsRepaid && installment.ExpectedDate >= pEvent.Date)
                        activeButton = true;
                }

                buttonSelectAGroupPerson.Visible = pEvent.Penalties > 0 || !activeButton ? false : true;
            }

            bool visible = ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies().Count > 1 &&
                           !_loan.Product.Currency.IsPivot && _exchangeRate != null;
            _lbECPrincipal.Text = visible
                                             ? string.Format("{0} {1}",(pEvent.Principal/ _exchangeRate.Rate).GetFormatedValue(_loan.UseCents),
                                                             ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code)
                                             : string.Empty;


            lbCInterest.Text = visible
                                           ? string.Format("{0} {1}", (pEvent.Interests / _exchangeRate.Rate).GetFormatedValue(_loan.UseCents), 
                                                            ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code)
                                           : string.Empty;


            _lbECFees.Text = visible
                                        ? string.Format("{0} {1}", (pEvent.Penalties / _exchangeRate.Rate).GetFormatedValue(_loan.UseCents), 
                                                            ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code)
                                        : string.Empty;

            _lbECCommissions.Text = visible
                                        ? string.Format("{0} {1}", (pEvent.Commissions / _exchangeRate.Rate).GetFormatedValue(_loan.UseCents), 
                                                            ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code) 
                                        : string.Empty;
        }

        private void SetInstalments(IEnumerable<Installment> pInstalments)
        {
            listViewRepayments.Items.Clear();
            foreach (Installment installment in pInstalments)
            {
                ListViewItem listViewItem = new ListViewItem(installment.Number.ToString());
                if (installment.IsRepaid)
                {
                    listViewItem.BackColor = Color.FromArgb(61, 153, 57);
                    listViewItem.ForeColor = Color.White;
                }
                if (installment.IsPending)
                {
                    listViewItem.BackColor = Color.Orange;
                    listViewItem.ForeColor = Color.White;
                }
                listViewItem.Tag = installment;
                listViewItem.SubItems.Add(installment.ExpectedDate.ToShortDateString());
                listViewItem.SubItems.Add(installment.InterestsRepayment.GetFormatedValue(_loan.UseCents));
                listViewItem.SubItems.Add(installment.CapitalRepayment.GetFormatedValue(_loan.UseCents));
                listViewItem.SubItems.Add(installment.AmountHasToPayWithInterest.GetFormatedValue(_loan.UseCents));

                listViewItem.SubItems.Add(ServicesProvider.GetInstance().GetGeneralSettings().IsOlbBeforeRepayment
                                              ? installment.OLB.GetFormatedValue(_loan.UseCents)
                                              : installment.OLBAfterRepayment.GetFormatedValue(_loan.UseCents));

                listViewItem.SubItems.Add(installment.PaidInterests.GetFormatedValue(_loan.UseCents));
                listViewItem.SubItems.Add(installment.PaidCapital.GetFormatedValue(_loan.UseCents));
                listViewItem.SubItems.Add(installment.PaidDate.HasValue
                                              ? installment.PaidDate.Value.ToShortDateString()
                                              : "-");
                listViewRepayments.Items.Add(listViewItem);
            }
        }

        private void checkBoxTotalAmount_CheckedChanged(object sender, EventArgs e)
        {
            OCurrency maximumAmount = _loan.CalculateMaximumAmountAuthorizedToRepay(_instalmentNumber, _date.Date,
                                                                                    _disableFees, _manualPenalties,
                                                                                    _manualCommission, _disableInterests,
                                                                                    _manualInterests,
                                                                                    _keepExpectedInstallment);

            OCurrency regradingAmount = _loan.CalculateAmountToRepaySpecifiedInstallment(_instalmentNumber,
                                                                                         TimeProvider.Today,
                                                                                         _disableFees, _manualPenalties,
                                                                                         _manualCommission,
                                                                                         _disableInterests,
                                                                                         _manualInterests,
                                                                                         _keepExpectedInstallment);

            _maximumAmount = _loan.CalculateMaximumAmountAuthorizedToRepay(_instalmentNumber, _date.Date, _disableFees,
                                                                           _manualPenalties, _manualCommission,
                                                                           _disableInterests, _manualInterests,
                                                                           _keepExpectedInstallment);

            nudICAmount.Maximum = maximumAmount.Value;
            nudICAmount.Value = checkBoxTotalAmount.Checked ? maximumAmount.Value : regradingAmount.Value;
        }

        private void _dateTimePicker_CloseUp(object sender, EventArgs e)
        {
            DateChanged();
        }

        private void DateChanged()
        {
            try
            {
                //_date = _dateTimePicker.Value;
                _date = new DateTime(dtpRepaymentDate.Value.Year, dtpRepaymentDate.Value.Month, dtpRepaymentDate.Value.Day);
                SetExchangeRate();
            }
            catch (Exception ex)
            {
                //_dateTimePicker.Value = TimeProvider.Today;
                //_date = _dateTimePicker.Value;
                _date = new DateTime(TimeProvider.Today.Year, TimeProvider.Today.Month, TimeProvider.Today.Day);
                dtpRepaymentDate.Invalidate();
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                SetExchangeRate();
            }
        }

        private void _dateTimePicker_KeyUp(object sender, KeyEventArgs e)
        {
            DateChanged();
        }

        private void textBoxCommission_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _manualCommission = ServicesHelper.ConvertStringToDecimal(textBoxCommission.Text, _loan.UseCents);
                if (checkBoxTotalAmount.Checked)
                    _amount = _amount - _repaymentEvent.Commissions + _manualCommission;

                if (_loan.EscapedMember != null)
                {
                    if (checkBoxFees.Checked)
                    {
                        _amount = _repaymentEvent.Principal + _repaymentEvent.Interests + _manualCommission;
                        nudICAmount.Value = _amount.Value;
                    }
                    else
                    {
                        _amount = _principal + _interest + _commission;
                        nudICAmount.Value = _amount.Value;
                    }
                }

                DisplayAmountLabel();
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                textBoxCommission.Text = @"0";
            }
        }

        private void _rbKeepNotInitialSchedule_Click(object sender, EventArgs e)
        {
            if(_rbKeepNotInitialSchedule.Checked)
            {
                try
                {
                    bool IsTotalPayment = false;

                    if (checkBoxTotalAmount.Checked)
                    {
                        checkBoxTotalAmount.Checked = false;
                        IsTotalPayment = true;
                    }

                    _keepExpectedInstallment = _rbKeepInitialSchedule.Checked;
                    _doProportionPayment = rbProportionPayment.Checked;

                    DiseableAutomaticsStatus();
                    DisplayInstallmentsAndEvent();

                    checkBoxTotalAmount.Checked = IsTotalPayment;
                }
                catch (Exception ex)
                {
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                    _rbKeepInitialSchedule.Checked = !_rbKeepInitialSchedule.Checked;
                }
            }
        }

        private void _rbKeepInitialSchedule_Click(object sender, EventArgs e)
        {
            if(_rbKeepInitialSchedule.Checked)
            {
                try
                {
                    bool IsTotalPayment = false;

                    if (checkBoxTotalAmount.Checked)
                    {
                        checkBoxTotalAmount.Checked = false;
                        IsTotalPayment = true;
                    }

                    _keepExpectedInstallment = _rbKeepInitialSchedule.Checked;
                    _doProportionPayment = rbProportionPayment.Checked;

                    DiseableAutomaticsStatus();
                    DisplayInstallmentsAndEvent();

                    checkBoxTotalAmount.Checked = IsTotalPayment;
                }
                catch (Exception ex)
                {
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                    _rbKeepInitialSchedule.Checked = !_rbKeepInitialSchedule.Checked;
                }
            }
        }

        private void _lbICAmountToGoBackNormal_DoubleClick(object sender, EventArgs e)
        {
            nudICAmount.Value = _regradingAmount.Value;
        }

        private void comboBoxPaymentMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            _paymentMethod = cmbPaymentMethod.SelectedItem as PaymentMethod;
            if (_paymentMethod != null) 
                checkBoxPending.Visible = _paymentMethod.IsPending;
            DisplayInstallmentsAndEvent();
        }

        private void checkBoxPending_CheckedChanged(object sender, EventArgs e)
        {
            DisplayInstallmentsAndEvent();
        }

        private void buttonSelectAGroupPerson_Click(object sender, EventArgs e)
        {
            List<Member> members =
                ServicesProvider.GetInstance().GetClientServices().FindHistoryPersonsByContract(((Group) _client).Id, _loan.Id);

            MembersOfGroup frmMembersOfTheGroup = new MembersOfGroup(members, _loan, dtpRepaymentDate.Value.Date);
            frmMembersOfTheGroup.ShowDialog();
            OCurrency memberRemainingAmount = frmMembersOfTheGroup.MemberRemainingAmount;

            if (memberRemainingAmount != 0)
            {
                _loan.EscapedMember = frmMembersOfTheGroup.Member;
                _escapedMember = frmMembersOfTheGroup.Member;
                _rbKeepInitialSchedule.Checked = false;
                _keepExpectedInstallment = false;
                _rbKeepNotInitialSchedule.Checked = true;

                gbTypeOfRepayment.Enabled = false;
                checkBoxTotalAmount.Enabled = false;
                nudICAmount.Enabled = false;
                nudECAmount.Enabled = false;
                checkBoxInterests.Enabled = false;
                _keepExpectedInstallment = false;
                nudICAmount.Value = memberRemainingAmount.Value;

                _principal = _repaymentEvent.Principal;
                _interest = _repaymentEvent.Interests;
                _commission = _repaymentEvent.Commissions;
            }
            else
            {
                _loan.EscapedMember = null;
            }
        }

        private void nudICAmount_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                SetAmount((NumericUpDown)sender, _loan.UseCents);
                DisplayInstallmentsAndEvent();
                _manualAmountWasEntered = true;
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                nudICAmount.Value = _loan.CalculateAmountToRepaySpecifiedInstallment(_instalmentNumber, TimeProvider.Today, _disableFees, _manualPenalties, 
                    _manualCommission, _disableInterests, _manualInterests, _keepExpectedInstallment).Value;
            }
        }

        private void nudECAmount_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                SetAmount((NumericUpDown)sender, true);
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                nudICAmount.Value = _loan.CalculateAmountToRepaySpecifiedInstallment(_instalmentNumber, TimeProvider.Today, _disableFees, _manualPenalties, 
                    _manualCommission, _disableInterests, _manualInterests, _keepExpectedInstallment).Value;
            }
        }

        private void textBoxInterest_Leave(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = ServicesHelper.ConvertStringToDecimal(tb.Text, _loan.UseCents).ToString();
        }

        private void _lbICAmountToGoBackNormal_MouseEnter(object sender, EventArgs e)
        {
            lblAmountToGoBackNormal.ForeColor = Color.Blue;
        }

        private void _lbICAmountToGoBackNormal_MouseLeave(object sender, EventArgs e)
        {
            lblAmountToGoBackNormal.ForeColor = Color.FromArgb(0, 88, 56);
        }

        private void rbProportionPayment_Click(object sender, EventArgs e)
        {
            try
            {
                _doProportionPayment = rbProportionPayment.Checked;
                _keepExpectedInstallment = false;
                DiseableAutomaticsStatus();
                DisplayInstallmentsAndEvent();
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
        }
    }
}
