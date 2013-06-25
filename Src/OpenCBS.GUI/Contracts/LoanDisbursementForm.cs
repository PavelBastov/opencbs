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
using System.Globalization;
using System.Windows.Forms;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Accounting;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Events;
using OpenCBS.ExceptionsHandler;
using OpenCBS.ExceptionsHandler.Exceptions.SavingExceptions;
using OpenCBS.GUI.Accounting;
using OpenCBS.GUI.UserControl;
using OpenCBS.Reports;
using OpenCBS.Services;
using OpenCBS.Shared;
using OpenCBS.Enums;

namespace OpenCBS.GUI.Contracts
{
    /// <summary>
    /// Summary description for CreditContractDisbursmentForm.
    /// </summary>
    public partial class LoanDisbursementForm : SweetBaseForm
    {
        private bool _disableFees;
        private DateTime _newStartDate;
        private LoanDisbursmentEvent _loanDisbursmentEvent;
        private Loan _loan;
        private ExchangeRate _exchangeRate;
        private bool _alignInstallmentsDatesOnRealDisbursmentDate;
        private bool _feeModified;

        public LoanDisbursementForm(Loan pLoan)
        {
            _loan = pLoan;
            _exchangeRate = null;
            InitializeComponent();
            Initialization();
        }

        public Loan Loan
        {
            get { return _loan; }
        }

        private void SetFeeAmount()
        {
            tbEntryFee.TextChanged -= TbEntryFeeTextChanged;
            tbEntryFee.Text = _loan.GetSumOfFees().GetFormatedValue(_loan.Product.Currency.UseCents);
            tbEntryFee.TextChanged += TbEntryFeeTextChanged;
        }

        private void CheckModification()
        {
            _disableFees = false;

            if (ServicesProvider.GetInstance().GetGeneralSettings().ModifyEntryFee)
            {
                try
                {
                    checkBoxFees.Enabled = ServicesProvider.GetInstance().GetContractServices().CanUserModifyEntryFees();
                    tbEntryFee.Enabled = ServicesProvider.GetInstance().GetContractServices().CanUserModifyEntryFees();
                    _lbFees.Enabled = ServicesProvider.GetInstance().GetContractServices().CanUserModifyEntryFees();
                    lblEntryFeeCurrency.Enabled = ServicesProvider.GetInstance().GetContractServices().CanUserModifyEntryFees();
                    InitializeExternalCurrency();
                }
                catch (Exception ex)
                {
                    checkBoxFees.Enabled = false;
                    tbEntryFee.Enabled = false;
                    _lbFees.Enabled = false;
                    lblEntryFeeCurrency.Enabled = false;
                    SetFeeAmount();
                    new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                }
            }
            else
            {
                tbEntryFee.Enabled = false;
                _lbFees.Enabled = false;
                lblEntryFeeCurrency.Enabled = false;
            }
        }

        private void Initialization()
        {
            _lbFundingLineValue.Text = _loan.FundingLine.Name;
            _alignInstallmentsDatesOnRealDisbursmentDate = true;
            InitializeDate();
            CheckDateChange();
            InitializeLoanDisburseEvent();
            FillComboBoxPaymentMethods();
            InitPrintButton();
            tbEntryFee.Enabled = ServicesProvider.GetInstance().GetGeneralSettings().ModifyEntryFee;
        }

        private void InitPrintButton()
        {
            Visibility visibility;
            switch (_loan.ClientType)
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

            btnPrint.Visibility = visibility;
            btnPrint.ReportInitializer
                = report =>
                      {
                          report.SetParamValue("user_id", User.CurrentUser.Id);
                          report.SetParamValue("contract_id", _loan.Id);
                      };
            btnPrint.LoadReports();
        }

        private void InitializeDate()
        {
            _newStartDate = _loan.StartDate;
        }

        private bool InitializeLoanDisburseEvent()
        {
            try
            {
                _loanDisbursmentEvent = ServicesProvider.GetInstance().GetContractServices().DisburseSimulation(_loan,
                                                                                                                _alignInstallmentsDatesOnRealDisbursmentDate,
                                                                                                                _newStartDate,
                                                                                                                _disableFees);

                if (_loanDisbursmentEvent != null)
                {
                    _lbAmountValue.Text = _loanDisbursmentEvent.Amount.GetFormatedValue(_loan.UseCents);
                    lblAmountCurrency.Text = _loan.Product.Currency.Code;
                    _lbLoanCodeValue.Text = _loan.Code;
                    if (!_feeModified)
                        SetFeeAmount();
                    lblEntryFeeCurrency.Text = _loan.Product.Currency.Code;
                    InitializeExternalCurrency();
                }
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
                _exchangeRate = null;
                InitializeExternalCurrency();
                return false;
            }
            return true;
        }

        private void InitializeExternalCurrency()
        {
            lblPivotCurrency.Visible = false;
            lblFeesCurrencyPivot.Visible = false;
            if (!_loan.Product.Currency.IsPivot && _exchangeRate != null)
            {
                lblPivotCurrency.Visible = true;
                lblFeesCurrencyPivot.Visible = true;
                OCurrency amount =
                    ServicesProvider.GetInstance().GetAccountingServices().ConvertAmountToExternalCurrency(
                        _loan.Amount, _exchangeRate);
                lblPivotCurrency.Text = amount.GetFormatedValue(_loan.UseCents);


                decimal value;
                decimal.TryParse(tbEntryFee.Text, out value);

                lblFeesCurrencyPivot.Text = _disableFees
                                                ? "0"
                                                : ServicesProvider.GetInstance().GetAccountingServices().
                                                      ConvertAmountToExternalCurrency(
                                                          value,
                                                          _exchangeRate).
                                                      GetFormatedValue(_loan.UseCents) + " ";
                string pivot = ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code;
                lblPivotCurrency.Text += pivot;
                lblFeesCurrencyPivot.Text += pivot;
            }
        }

        private void FillComboBoxPaymentMethods()
        {
            List<PaymentMethod> paymentMethods =
                ServicesProvider.GetInstance().GetPaymentMethodServices().GetAllPaymentMethods();
            foreach (PaymentMethod method in paymentMethods)
                cmbPaymentMethod.Items.Add(method);

            cmbPaymentMethod.SelectedItem = paymentMethods[0];
        }

        private void ButtonSaveClick(object sender, EventArgs e)
        {
            if (!CheckDateChange())
                return;
            _loan.Comments = tbComment.Text;
            try
            {
                DistributeEntryFees();
                // If disbursement goes to savings
                if (cmbPaymentMethod.Text == OPaymentMethods.Savings.ToString())
                {
                    if (_loan.CompulsorySavings == null)
                    {
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.NoCompulsorySavings);
                    }

                    if (_loan.CompulsorySavings.Status == OSavingsStatus.Active)
                    {
                        _loan = ServicesProvider.GetInstance().GetContractServices().Disburse(_loan, _newStartDate,
                                                                                              _alignInstallmentsDatesOnRealDisbursmentDate,
                                                                                              _disableFees,
                                                                                              (PaymentMethod)
                                                                                              cmbPaymentMethod.
                                                                                                  SelectedItem);
                        ServicesProvider.GetInstance().GetSavingServices().LoanDisbursement(_loan.CompulsorySavings,
                                                                                            _loan,
                                                                                            _newStartDate,
                                                                                            "Disbursement of the loan: " +
                                                                                            _loan.Code, User.CurrentUser,
                                                                                            checkBoxFees.Checked);
                    }
                    else
                    {
                        throw new OpenCbsSavingException(OpenCbsSavingExceptionEnum.CompulsorySavingsContractIsNotActive);
                    }
                }
                else
                {
                    _loan = ServicesProvider.GetInstance().GetContractServices().Disburse(_loan, _newStartDate,
                                                                                          _alignInstallmentsDatesOnRealDisbursmentDate,
                                                                                          _disableFees,
                                                                                          (PaymentMethod)
                                                                                          cmbPaymentMethod.SelectedItem);
                }
            }
            catch (Exception ex)
            {
                _loan.Disbursed = false;
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
            finally
            {
                Close();
            }
        }

        private void DistributeEntryFees()
        {
            if (_loan.LoanEntryFeesList != null && _loan.LoanEntryFeesList.Count == 1)
            {
                _loan.LoanEntryFeesList[0].ProductEntryFee.IsRate = false;
                _loan.LoanEntryFeesList[0].FeeValue = Convert.ToDecimal(tbEntryFee.Text);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _loan.Disbursed = false;
            Close();
        }

        private void CheckBoxFeesCheckedChanged(object sender, EventArgs e)
        {
            _disableFees = checkBoxFees.Checked;
            tbEntryFee.Enabled = !checkBoxFees.Checked && ServicesProvider.GetInstance().GetGeneralSettings().ModifyEntryFee;
            
            tbEntryFee.Text = _disableFees
                                        ? "0"
                                        : _loan.GetSumOfFees().GetFormatedValue(_loan.Product.Currency.UseCents);
            lblEntryFeeCurrency.Text = _loan.Product.Currency.Code;


            lblFeesCurrencyPivot.Text = _disableFees
                                              ? "0"
                                              : _exchangeRate != null
                                                    ? ServicesProvider.GetInstance().GetAccountingServices().
                                                          ConvertAmountToExternalCurrency(_loan.GetSumOfFees(),
                                                                                          _exchangeRate).
                                                          GetFormatedValue(_loan.UseCents)
                                                    : "0";
            lblFeesCurrencyPivot.Text += ServicesProvider.GetInstance().GetCurrencyServices().GetPivot().Code;
        }

        private bool CheckDateChange()
        {
            _exchangeRate = null;
            if (ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies().Count == 1)
            {
                _exchangeRate = new ExchangeRate
                                    {
                                        Currency = _loan.Product.Currency,
                                        Date = _newStartDate,
                                        Rate = 1
                                    };
            }
            buttonSave.Enabled = false;

            try
            {
                if (!ServicesProvider.GetInstance().GetExchangeRateServices().RateExistsForEachCurrency
                         (ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies(), _newStartDate))
                {
                    buttonAddExchangeRate.Enabled = lblPivotCurrency.Enabled;
                    var xrForm =
                        new ExchangeRateForm(new DateTime(_newStartDate.Year, _newStartDate.Month, _newStartDate.Day),
                                             _loan.Product.Currency);
                    xrForm.ShowDialog();
                }
                _exchangeRate = ServicesProvider.GetInstance().GetAccountingServices().FindExchangeRate(_newStartDate,
                                                                                                        _loan.Product.
                                                                                                            Currency);
            }
            catch (Exception ex)
            {
                new frmShowError(CustomExceptionHandler.ShowExceptionText(ex)).ShowDialog();
            }
            finally
            {
                if (_exchangeRate != null)
                {
                    buttonSave.Enabled = true;
                    lblPivotCurrency.Visible =
                        ServicesProvider.GetInstance().GetCurrencyServices().FindAllCurrencies().Count > 1 &&
                        !_loan.Product.Currency.IsPivot;
                    lblFeesCurrencyPivot.Visible = lblPivotCurrency.Visible;
                    buttonAddExchangeRate.Visible = false;
                }
                else
                {
                    buttonSave.Enabled = false;
                    buttonAddExchangeRate.Enabled = true;
                    lblPivotCurrency.Visible = true;
                    lblFeesCurrencyPivot.Visible = true;
                }
            }
            bool lde = InitializeLoanDisburseEvent();
            return lde;
        }

        private void ButtonAddExchangeRateClick(object sender, EventArgs e)
        {
            ExchangeRateForm exchangeRate = new ExchangeRateForm(_newStartDate, _loan.Product.Currency);
            exchangeRate.ShowDialog();

            _exchangeRate = exchangeRate.ExchangeRate;
            InitializeExternalCurrency();
            buttonSave.Enabled = _exchangeRate != null;
        }

        private void TbEntryFeeValueKeyPress(object sender, KeyPressEventArgs e)
        {
            int keyCode = e.KeyChar;

            if ((keyCode >= 48 && keyCode <= 57) || (keyCode == 8) || (Char.IsControl(e.KeyChar) && e.KeyChar
                                                                       != ((char) Keys.V | (char) Keys.ControlKey)) ||
                (Char.IsControl(e.KeyChar) && e.KeyChar !=
                 ((char) Keys.C | (char) Keys.ControlKey)) ||
                (e.KeyChar.ToString() == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
            {
                e.Handled = false;
            }
            else
                e.Handled = true;
        }

        private void TbEntryFeeTextChanged(object sender, EventArgs e)
        {
            CheckModification();
            _feeModified = true;
        }
    }
}
