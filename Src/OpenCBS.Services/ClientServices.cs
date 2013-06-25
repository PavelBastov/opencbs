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
using System.Data;
using System.Linq;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.Contracts.Loans.Installments;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.SearchResult;
using OpenCBS.DatabaseConnection;
using OpenCBS.Enums;
using System.Data.SqlClient;
using OpenCBS.ExceptionsHandler;
using OpenCBS.Manager.Clients;
using OpenCBS.Shared;
using OpenCBS.ExceptionsHandler.Exceptions.FundingLineExceptions;
using OpenCBS.Shared.Settings;
using OpenCBS.Services.Rules;

namespace OpenCBS.Services
{
    /// <summary>
    /// Description r�sum�e de ClientServices.
    /// </summary>
    [Security()]
    public class ClientServices : ContextBoundObject
    {
        private readonly ClientManager _clientManagement;

        private static readonly string TESTDB = "opencbs_test";
        private readonly ConnectionManager _connectionManager;
        private readonly int ERRORVALUE = -1;
        private readonly ApplicationSettings _dataParam;
        private readonly User _user = new User();
        private readonly PicturesServices _picturesServices;
        private readonly LoanServices _loanServices = ServicesProvider.GetInstance().GetContractServices();

        public ClientServices(User pUser)
        {
            _user = pUser;
            _clientManagement = new ClientManager(pUser, true, true);
            _clientManagement.ClientSelected += ClientSelected;
            _dataParam = ApplicationSettings.GetInstance(pUser.Md5);
            _picturesServices = new PicturesServices(pUser);
        }

        public ClientServices(User pUser, string testDB)
        {
            _user = pUser;
            _clientManagement = new ClientManager(testDB);
            _dataParam = ApplicationSettings.GetInstance(pUser.Md5);
            _picturesServices = new PicturesServices(testDB);
        }

        // MOCK
        public ClientServices(ClientManager clientManagement)
        {
            _clientManagement = clientManagement;
            _connectionManager = ConnectionManager.GetInstance(TESTDB);
            _dataParam = ApplicationSettings.GetInstance("");
            _picturesServices = new PicturesServices(TESTDB);
            _dataParam.DeleteAllParameters();
            _dataParam.AddParameter("GROUP_MIN_MEMBERS", 4);
            _dataParam.AddParameter("GROUP_MAX_MEMBERS", 10);
            _dataParam.AddParameter(OGeneralSettings.ALLOWSMULTIPLEGROUPS, 0);
            _dataParam.AddParameter("CITY_MANDATORY", true);
        }

        // MOCK
        public ClientServices(ApplicationSettings dataParam)
        {
            _clientManagement = new ClientManager(TESTDB);
            _connectionManager = ConnectionManager.GetInstance(TESTDB);
            _picturesServices = new PicturesServices(TESTDB);
            _dataParam = dataParam;
        }

        // MOCK
        public ClientServices(ClientManager clientManagement, ConnectionManager connectionManager)
        {
            _clientManagement = clientManagement;
            _connectionManager = connectionManager;
        }

        public List<ContactRole> FindRoleAll()
        {
            return _clientManagement.SelectRoleAll();
        }

        public void SaveCorporatePerson(int pCorporateId, List<Contact> contacts)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction transac = connection.BeginTransaction())
            {
                try
                {
                    _clientManagement.DeleteBodyPersonByCorporate(pCorporateId, transac);

                    foreach (Contact contact in contacts)
                    {
                        _clientManagement.AddBodyCorporatePerson(pCorporateId, contact, transac);
                    }
                    transac.Commit();
                }
                catch (Exception ex)
                {
                    transac.Rollback();
                    throw ex;
                }
            }
        }

        public List<ClientSearchResult> FindTiers(out int numbersTotalPage, out int numberOfRecords,
                                                   string query, int isActive, int currentlyPage,
                                                   int includePersons, int includeGroups, int includeVillages)
        {
            List<ClientSearchResult> result = new List<ClientSearchResult>();

            numberOfRecords = _clientManagement.GetFoundRecordsNumber(query, isActive, includePersons, includeGroups, includeVillages);
            //numberOfRecords = _ClientManagement.GetNumberVillages(query);
            result.AddRange(_clientManagement.SearchAllByCriteres(query, isActive, currentlyPage, includePersons, includeGroups, includeVillages));

            //numberOfRecords += _ClientManagement.GetNumberGroup(query);
            //result.AddRange(_ClientManagement.SearchGroupByCriteres(currentlyPage, query));

            //numberOfRecords += _ClientManagement.GetNumberPerson(query, isActive);
            //result.AddRange(_ClientManagement.SearchPersonByCriteres(currentlyPage, query));

            numbersTotalPage = numberOfRecords / 20;
            numbersTotalPage += numberOfRecords % 20 > 0 ? 1 : 0;
            return result;
        }
        public List<Person> SelectAllPersons ()
        {
            return _clientManagement.FindAllPersons();
        }

        public List<ClientSearchResult> FindInactivePersons(int currentPage, out int totalPages, out int totalRecords, string query)
        {
            totalRecords = _clientManagement.GetNumberInactivePersons(query);
            totalPages = 0 == totalRecords % 20 ? totalRecords / 20 : totalRecords / 20 + 1;
            return _clientManagement.SearchInactivePersonsByCriteres(currentPage, query);
        }

        public List<ClientSearchResult> FindTiersCorporates(int onlyActive, int currentlyPage, out int numbersTotalPage,
            out int numberOfRecords, string pQuery)
        {
            List<ClientSearchResult> result = new List<ClientSearchResult>();
            numberOfRecords = _clientManagement.GetNumberCorporate(pQuery);
            result.AddRange(_clientManagement.SearchCorporateByCriteres(onlyActive, currentlyPage, pQuery));
            numbersTotalPage = (numberOfRecords / 20) + 1;
            return result;
        }

        public List<ClientSearchResult> FindAllTiers(out int numberOfRecords, string pQuery)
        {
            List<ClientSearchResult> result = new List<ClientSearchResult>();
            numberOfRecords = _clientManagement.GetNumberOfRecordsFoundForSearchGroups(pQuery);
            result.AddRange(_clientManagement.SearchAllGroupsInDatabase(pQuery));

            numberOfRecords += _clientManagement.GetNumberOfRecordsFoundForSearchPersons(pQuery);
            result.AddRange(_clientManagement.SearchAllPersonsInDatabase(pQuery));

            return result;
        }

        public DataSet CreateContractHistoryDataSet(List<Loan> pList)
        {
            DataSet dataset = new DataSet();
            dataset.Tables.Add(new DataTable("ContractInformation"));
            dataset.Tables.Add(new DataTable("ContractSchedule"));

            dataset.Tables[0].Columns.Add("beneficiary_name", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_code", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_amount", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_interestRate", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_installmentType", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_maturity", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_creationDate", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_startDate", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_closeDate", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("loanofficer_name", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_rescheduled", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_gracePeriod", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_writtenOff", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_collateral_amount", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_collateral_name", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_badLoan", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_commentsOfEnd", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("product_name", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_daysLate", System.Type.GetType("System.String"));
            dataset.Tables[0].Columns.Add("contract_district_name", System.Type.GetType("System.String"));

            dataset.Tables[1].Columns.Add("number", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("expected_date", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("paid_date", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("interest_repayment", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("capital_repayment", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("installmentTotal", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("olb", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("paid_interest", System.Type.GetType("System.String"));
            dataset.Tables[1].Columns.Add("paid_principal", System.Type.GetType("System.String"));

            foreach (Loan contract in pList)
            {
                DataRow row = dataset.Tables[0].NewRow();
                // row[0] = contract.Beneficiary.Name;
                row[1] = contract.Code;
                row[2] = contract.Amount.ToString();
                row[3] = contract.InterestRate.ToString();
                row[4] = contract.InstallmentType.Name;
                row[5] = contract.NbOfInstallments.ToString();
                row[6] = contract.CreationDate.ToShortDateString();
                row[7] = contract.StartDate.ToShortDateString();
                row[8] = contract.CloseDate.ToShortDateString();
                row[9] = contract.LoanOfficer.Name;
                row[10] = contract.Rescheduled;
                row[11] = contract.GracePeriod.HasValue ? contract.GracePeriod.Value : 0;
                row[12] = contract.WrittenOff;

                //row[13] = contract.CollateralAmount;
                //row[14] = contract.Collateral != null ? contract.Collateral.Name : "-";
                row[15] = contract.BadLoan;
                //row[16] = contract.CommentsOfEnd;
                row[17] = contract.Product.Name;
                row[18] = contract.CalculatePastDueSinceLastRepayment(TimeProvider.Today);
                // row[19] = contract.Beneficiary.District.Name;
                dataset.Tables[0].Rows.Add(row);

                foreach (Installment installment in contract.InstallmentList)
                {
                    DataRow row2 = dataset.Tables[1].NewRow();
                    row2[0] = installment.Number;
                    row2[1] = installment.ExpectedDate.ToShortDateString();
                    if (installment.PaidDate.HasValue)
                        row2[2] = installment.PaidDate.Value.ToShortDateString();
                    else
                        row2[2] = "-";
                    row2[3] = installment.InterestsRepayment.ToString();
                    row2[4] = installment.CapitalRepayment.ToString();
                    row2[5] = installment.AmountHasToPayWithInterest.ToString();
                    row2[6] = installment.OLB.ToString();
                    row2[7] = installment.PaidInterests.ToString();
                    row2[8] = installment.PaidCapital.ToString();
                    dataset.Tables[1].Rows.Add(row2);
                }
            }
            return dataset;
        }

        public IClient FindTiersByContractId(int pContractId)
        {
            IClient client = _clientManagement.SelectClientByContractId(pContractId);

//            foreach (Project project in client.Projects)
//            {
//                foreach (Loan credit in project.Credits)
//                {
//                     credit.Corporate = _ClientManagement.SelectBodyCorporateById(credit.Corporate.Id);
//                    credit.FillStockAccountBalance();
//                }
//            }
            return client;
        }

        public IClient FindTiersBySavingsId(int pSavingsId)
        {
            return _clientManagement.SelectClientBySavingsId(pSavingsId);
        }

        public IClient FindTiers(int tiersId, OClientTypes type)
        {
            IClient client = null;
            if (type == OClientTypes.Corporate)
                client = _clientManagement.SelectBodyCorporateById(tiersId);
            if (type == OClientTypes.Group)
                client = _clientManagement.SelectGroupById(tiersId);
            if (type == OClientTypes.Person)
                client = _clientManagement.SelectPersonById(tiersId);
            if (OClientTypes.Village == type)
                client = _clientManagement.SelectVillageById(tiersId);
            
            if (client!=null)
                client.ActiveLoans = _loanServices.GetActiveLoans(client.Id);
            return client;
        }

        public Group FindGroupById(int groupId)
        {
            var group = _clientManagement.SelectGroupById(groupId);
            group.ActiveLoans=_loanServices.GetActiveLoans(groupId);
            return group;
        }

        public Person FindPersonByName(string name)
        {
            var person = _clientManagement.SelectPersonByName(name);
            person.ActiveLoans = _loanServices.GetActiveLoans(person.Id);
            return person;
        }

        public Person FindPersonById(int personId)
        {
            var person = _clientManagement.SelectPersonById(personId);
            person.ActiveLoans = _loanServices.GetActiveLoans(personId);
            return person;
        }

        public List<Member> FindGroupHistoryPersons(int groupId)
        {
            return _clientManagement.SelectGroupMembersByGroupId(groupId);
        }

        public List<VillageMember> FindVillageHistoryPersons(int villageId)
        {
            List<VillageMember> villageMembers= _clientManagement.SelectVillageMembersByVillageId(villageId, null);
            foreach (VillageMember member in villageMembers)
            {
                member.Tiers = _clientManagement.SelectPersonById(member.Tiers.Id);
            }
            return villageMembers;
        }

        public void GetNotRemovedVillageMembers(Village village)
        {
            List<VillageMember> members = _clientManagement.SelectVillageMembersByVillageId(village.Id, true);
            foreach (VillageMember member in members)
            {
                member.Tiers = _clientManagement.SelectPersonById(member.Tiers.Id);
                member.ActiveLoans =
                    ServicesProvider.GetInstance().GetContractServices().FindActiveContracts(member.Tiers.Id);
                village.AddMember(member);
            }
        }

        public List<Member> FindHistoryPersons(int groupId)
        {
            return _clientManagement.SelectHistoryPersonsInAGroup(groupId);
        }

        public List<Member> FindHistoryPersonsByContract(int pGroupId, int pContractId)
        {
            return _clientManagement.SelectHistoryPersonsInAGroup(pGroupId, pContractId);
        }

        public bool CheckIfTiersIsValid(IClient client)
        {
            if (client == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsNull);
            return true;
        }

        public OCurrency CalculateLoanShareAmount(int numberOfmembers, OCurrency amount)
        {
            return amount / (double)numberOfmembers;
        }

        public bool SecondaryAddressCorrectlyFilled(IClient client)
        {
            bool result = true;
            if (!client.SecondaryAddressIsEmpty && client.SecondaryAddressPartiallyFilled)
            {
                result = false;
                if (client.SecondaryDistrict == null)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.SecondaryDistrictIsNull);

                if (client.SecondaryDistrict.Id == 0)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.SecondaryDistrictIsBad);

                if (_dataParam.IsCityMandatory)
                {
                    if (client.SecondaryCity == null)
                        throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.SecondaryCityIsNull);
                }
                else
                    result = true;
            }
            return result;
        }
        public void ModifyBadClient()
        {
            return;
        }
        
        public string SavePerson(ref Person person)
        {
            return SavePerson(ref person, null);
        }

        public string SavePerson(ref Person person, Action<SqlTransaction, int> action)
        {
            string result = String.Empty;
            CheckPersonFilling(person);

            if (SecondaryAddressCorrectlyFilled(person))
            {
                if (person.Id == 0)
                {
                    if (CheckIfIdentificationDataAlreadyExists(person.IdentificationData, 0))
                        throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.IdentificationDataAlreadyUsed);

                    person.SetStatus();
                    person.Id = AddPerson(person, action);
                    SavePicture(person);
                }
                else
                {
                    if (CheckIfIdentificationDataAlreadyExists(person.IdentificationData, person.Id))
                        throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.IdentificationDataAlreadyUsed);
                    UpdatePerson(person, action);
                    SavePicture(person);
                }
            }
            return result;
        }

        private void CheckPersonFilling(Person person)
        {
            if (person.Activity == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.EconomicActivityIsNull);
            /*
            if (person.LoanCycle == 0)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LoanCycleIsEmpty);
            */
            if (person.District == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.DistrictIsNull);

            if (person.District.Id == 0)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.DistrictIsBad);

            if (_dataParam.IsCityMandatory)
            {
                if (string.IsNullOrEmpty(person.City))
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.CityIsNull);
            }

            if (person.FirstName == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.FirstNameIsNull);


            if (person.Sex == 0)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.SexIsNull);

            if (!_dataParam.IsAutomaticID && person.IdentificationData == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.IdentificationDataIsNull);

            if (_dataParam.EnforceIDPattern)
            {
                if (!new RegExCheckerServices(_user).CheckID(person.IdentificationData))
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.WrongIdPattern);
            }

            if (person.LastName == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LastNameIsEmpty);

            if (person.NbOfDependents.HasValue && person.NbOfDependents.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NbOfDependantsIsBadlyInformed);

            if (person.NbOfChildren.HasValue && person.NbOfChildren.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NbOfChildrensIsBadlyInformed);

            if (person.ChildrenBasicEducation.HasValue && person.ChildrenBasicEducation.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NbOfChidrensWithBasicEducationisBadlyInformed);

            if (person.Experience.HasValue && person.Experience.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.ExperienceIsBadlyInformed);

            if (person.NbOfPeople.HasValue && person.NbOfPeople.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NbOfPeopleWorkingWithinIsBadlyInformed);

            if (person.OtherOrgAmount.HasValue && person.OtherOrgAmount.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.OtherOrganizationAmountIsBadlyInformed);

            if (person.OtherOrgDebts.HasValue && person.OtherOrgDebts.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.OtherOrganizationDebtsIsBadlyInformed);

            if (person.HomeSize.HasValue && person.HomeSize.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.HouseSizeIsBadlyInformed);

            if (person.HomeTimeLivingIn.HasValue && person.HomeTimeLivingIn.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.HouseTimeLivingInIsBadlyInformed);

            if (person.LandplotSize.HasValue && person.LandplotSize.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LandPlotSizeIsBadlyInformed);

            if (person.LivestockNumber.HasValue && person.LivestockNumber.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LivestockNumberIsBadlyInformed);

            if (person.DateOfBirth != null)
            {
                int year = DateTime.Now.Year - person.DateOfBirth.Value.Year;

                if ((DateTime.Now.Month - person.DateOfBirth.Value.Month == 0 && DateTime.Now.Day - person.DateOfBirth.Value.Day < 0)
                    || DateTime.Now.Month - person.DateOfBirth.Value.Month < 0)
                    --year;

                if (year > _dataParam.ClientAgeMax || year < _dataParam.ClientAgeMin)
                {
                    List<string> temp = new List<string>();
                    temp.Add(_dataParam.ClientAgeMin.ToString());
                    temp.Add(_dataParam.ClientAgeMax.ToString());
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.AgeIsNotInRange, temp);
                }
            }
            else
            {
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.BirthDateIsWrong);    
            }
            
            if (null == person.Branch)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.BranchIsEmpty);
        }

        private void SavePicture(Person person)
        {
            if (person.IsImageUpdated)
                _picturesServices.UpdatePicture("PERSON", person.Id, 0, person.ImagePath);
            if (person.IsImageDeleted)
                _picturesServices.DeletePicture("PERSON", person.Id, 0);
            if (!string.IsNullOrEmpty(person.ImagePath) && person.IsImageAdded)
                _picturesServices.AddPicture("PERSON", person.Id, 0, person.ImagePath);

            if (person.IsImage2Updated)
                _picturesServices.UpdatePicture("PERSON", person.Id, 1, person.Image2Path);
            if (person.IsImage2Deleted)
                _picturesServices.DeletePicture("PERSON", person.Id, 1);
            if (!string.IsNullOrEmpty(person.Image2Path) && person.IsImage2Added)
                _picturesServices.AddPicture("PERSON", person.Id, 1, person.Image2Path);
        }

        private void SavePicture(Village village)
        {
            if (village.IsImageUpdated)
                _picturesServices.UpdatePicture("VILLAGE", village.Id, 0, village.ImagePath);
            if (village.IsImageDeleted)
                _picturesServices.DeletePicture("VILLAGE", village.Id, 0);
            if (!string.IsNullOrEmpty(village.ImagePath) && village.IsImageAdded)
                _picturesServices.AddPicture("VILLAGE", village.Id, 0, village.ImagePath);

            if (village.IsImage2Updated)
                _picturesServices.UpdatePicture("VILLAGE", village.Id, 1, village.Image2Path);
            if (village.IsImage2Deleted)
                _picturesServices.DeletePicture("VILLAGE", village.Id, 1);
            if (!string.IsNullOrEmpty(village.Image2Path) && village.IsImage2Added)
                _picturesServices.AddPicture("VILLAGE", village.Id, 1, village.Image2Path);
        }

        private void SavePicture(Group group)
        {
            if (group.IsImageUpdated)
                _picturesServices.UpdatePicture("GROUP", group.Id, 0, group.ImagePath);
            if (group.IsImageDeleted)
                _picturesServices.DeletePicture("GROUP",group.Id,0);
            if (!string.IsNullOrEmpty(group.ImagePath) && group.IsImageAdded)
                _picturesServices.AddPicture("GROUP", group.Id, 0, group.ImagePath);

            if (group.IsImage2Updated)
                _picturesServices.UpdatePicture("GROUP", group.Id, 1, group.Image2Path);
            if (group.IsImage2Deleted)
                _picturesServices.DeletePicture("GROUP", group.Id, 1);
            if (!string.IsNullOrEmpty(group.Image2Path) && group.IsImage2Added)
                _picturesServices.AddPicture("GROUP", group.Id, 1, group.Image2Path);
        }

        private void SavePicture(Corporate corporate)
        {
            if (corporate.IsImageUpdated)
                _picturesServices.UpdatePicture("CORPORATE", corporate.Id, 0, corporate.ImagePath);
            if (corporate.IsImageDeleted)
                _picturesServices.DeletePicture("CORPORATE", corporate.Id, 0);
            if (!string.IsNullOrEmpty(corporate.ImagePath) && corporate.IsImageAdded)
                _picturesServices.AddPicture("CORPORATE", corporate.Id, 0, corporate.ImagePath);

            if (corporate.IsImage2Updated)
                _picturesServices.UpdatePicture("CORPORATE", corporate.Id, 1, corporate.Image2Path);
            if (corporate.IsImage2Deleted)
                _picturesServices.DeletePicture("CORPORATE", corporate.Id, 1);
            if (!string.IsNullOrEmpty(corporate.Image2Path) && corporate.IsImage2Added)
                _picturesServices.AddPicture("CORPORATE", corporate.Id, 1, corporate.Image2Path);
        }

        public bool CheckIfIdentificationDataAlreadyExists(string pIdentificationData, int pId)
        {
            return _clientManagement.IsThisIdentificationDataAlreadyUsed(pIdentificationData, pId);
        }

        public List<Person> CheckIfThereIsSimilarIdentificationData(string pIdentificationData, int pID)
        {
            return _clientManagement.IsThereSimilardentificationDataAlreadyUsed(pIdentificationData, pID);
        }

        private int AddPerson(Person pPerson, Action<SqlTransaction, int> action)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction transac = connection.BeginTransaction())
            {
                pPerson.Scoring = 0.6;
                try
                {
                    string fnFormat = string.Format(@"{{0:{0}}}", _dataParam.FirstNameFormat);
                    string lnFormat = string.Format(@"{{0:{0}}}", _dataParam.LastNameFormat);

                    pPerson.FirstName = string.Format(fnFormat, pPerson.FirstName);
                    pPerson.LastName = string.Format(lnFormat, pPerson.LastName);

                    if (pPerson.IdentificationData == null)
                    {
                        pPerson.IdentificationData = "test";
                        pPerson.Id = _clientManagement.AddPerson(pPerson, transac);
                        pPerson.IdentificationData = pPerson.Id.ToString();
                        _clientManagement.UpdatePersonIdentificationData(pPerson.Id, transac);
                    }
                    else
                    {
                        pPerson.Id = _clientManagement.AddPerson(pPerson, transac);
                    }

                    if (action != null) action(transac, pPerson.Id);

                    transac.Commit();
                    return pPerson.Id;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    transac.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdatePerson(Person person, Action<SqlTransaction, int> action)
        {
            IClient oldPerson = _clientManagement.SelectPersonById(person.Id);
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction transac = connection.BeginTransaction())
            try
            {
                _clientManagement.UpdatePerson(person, transac);
                UpdateClientBranchHistory(person, oldPerson, transac);

                if (action != null) action(transac, person.Id);
                transac.Commit();
            }
            catch (Exception ex)
            {
                transac.Rollback();
                throw ex;
            }
        }

        public bool ClientIsAPerson(IClient client)
        {
            bool status = false;
            if (client != null)
            {
                if (client is Group)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsGroup);
                if (client is Corporate)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsCorporate);
                else if (client is Person)
                    status = true;
            }
            return status;
        }

        public bool ClientIsAGroup(IClient client)
        {
            bool result = false;
            if (client != null)
            {
                if (client is Person)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsPerson);
                if (client is Corporate)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsCorporate);
                else if (client is Group)
                    result = true;
            }
            return result;
        }


        public bool ClientIsACorporate(IClient client)
        {
            bool result = false;
            if (client != null)
            {
                if (client is Group)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsGroup);
                if (client is Person)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsPerson);
                else if (client is Corporate)
                    result = true;
            }
            return result;
        }

        public void ClientsNumberGuarantee(IClient client)
        {
            if (GetGuaranteeMaxLoanCover(client) >= ServicesProvider.GetInstance().GetGeneralSettings().MaxLoansCovered)
            {
                List<string> temp = new List<string>();
                temp.Add(ServicesProvider.GetInstance().GetGeneralSettings().MaxLoansCovered.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.GuarantorMaxLoansCoveredExceed, temp);
            }
        }

        public int GetGuaranteeMaxLoanCover(IClient client)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            return _clientManagement.GetNumberOfGuarantorsLoansCover(client.Id, sqlTransac);
        }

        public void ClientIsActive(IClient client)
        {
            if (client is Group)
            {
                foreach (Member member in ((Group)client).Members)
                {
                    if (member.Tiers.Active)
                        throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonIsActive);
                }
            }
            else
            {
                if (client.Active)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonIsActive);
            }
        }

        public void UpdateClientStatus(IClient pClient)
        {
            using (SqlConnection conn = _clientManagement.GetConnection())
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                UpdateClientStatus(pClient, transaction);
                transaction.Commit();
            }
        }

        public void UpdateClientStatus(IClient pClient, SqlTransaction sqlTransaction)
        {
            _clientManagement.UpdateClientStatus(pClient, sqlTransaction);
        }

        public List<Group> FindAllGroupsWhereSelectedPersonIsAMember(int pPersonId)
        {
            return _clientManagement.SelectAllGroupsWhereSelectedPersonIsAMember(pPersonId);
        }

        public int GetPersonGroupCount(int personId)
        {
            return _clientManagement.GetPersonGroupCount(personId);
        }

        public List<Village> FindAllNSGsWhereSelectedPersonIsAMember(int pPersonId)
        {
            return _clientManagement.SelectAllNSGsWhereSelectedPersonIsAMember(pPersonId);
        }

        public bool ClientCanBeAddToAGroup(IClient client, Group group)
        {
            Person person = (Person) client;

            var personId = person.Id;
            foreach (Member selectedPerson in group.Members)
            {
                if (selectedPerson.Tiers.Id == personId)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonAlreadyInThisGroup);
            }

            if (person.Active && !ApplicationSettings.GetInstance(_user != null ? _user.Md5 : "").IsAllowMultipleGroups)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonIsActive);
            
            if (_clientManagement.IsLeaderIsActive(personId))
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonIsALeader);

            CheckPersonGroupCount(personId);

            return true;
        }

        public void CheckPersonGroupCount(int personId)
        {
            if (!ServicesProvider.GetInstance().GetGeneralSettings().IsAllowMultipleGroups
                && GetPersonGroupCount(personId) > 0)
            {
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonIsInTheGroup);
            }
        }

        public bool ClientCanBeAddToCorporate(IClient client, Corporate corporate)
        {
            Person person = (Person)client;

            if (corporate.Contacts.Any(selectedPerson => selectedPerson.Tiers.Id == person.Id))
            {
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonAlreadyInThisGroup);
            }

            if (person.Active && !ApplicationSettings.GetInstance(_user != null ? _user.Md5 : "").IsAllowMultipleGroups)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.PersonIsActive);

            return true;
        }

        public void CheckMinNumberOfMembers(Group group)
        {
            if (group.GetNumberOfMembers == _dataParam.GroupMinMembers)
            {
                List<string> L = new List<string>();
                L.Add(_dataParam.GroupMinMembers.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NoEnoughPersonsInThisGroup, L);
            }
        }

        public void CheckMaxNumberOfMembers(Group group)
        {
            if (group.GetNumberOfMembers == _dataParam.GroupMaxMembers)
            {
                List<string> L = new List<string>();
                L.Add(_dataParam.GroupMaxMembers.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TooMuchPersonsInThisGroup, L);
            }
        }

        public string SaveSolidarityGroup(ref Group group)
        {
            return SaveSolidarityGroup(ref group, null);
        }

        public string SaveSolidarityGroup(ref Group group, Action<SqlTransaction, int> action)
        {
            string status = ValidateSolidarityGroup(group);

            if (SecondaryAddressCorrectlyFilled(group))
            {
                if (group.Id == 0)
                {
                    group.Id = AddGroup(group, action);
                    SavePicture(group);
                }
                else
                {
                    UpdateGroup(group, action);
                    SavePicture(group);
                }
            }

            return status;
        }

        private string ValidateSolidarityGroup(Group group)
        {
            string status = String.Empty;
            if (group == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsNull);

            if (group.Name == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NameIsEmpty);

            if (group.District == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.DistrictIsNull);

            if (group.District.Id == 0)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.DistrictIsBad);

            if (_dataParam.IsCityMandatory)
            {
                if (group.City == null)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.CityIsNull);
            }

            if (group.GetNumberOfMembers < _dataParam.GroupMinMembers)
            {
                List<string> L = new List<string>();
                L.Add(_dataParam.GroupMinMembers.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NoEnoughPersonsInThisGroup, L);
            }

            if (group.GetNumberOfMembers > _dataParam.GroupMaxMembers)
            {
                List<string> L = new List<string>();
                L.Add(_dataParam.GroupMaxMembers.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TooMuchPersonsInThisGroup, L);
            }

            if (group.Leader == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LeaderIsEmpty);

            if (group.Leader.Tiers.Id == 0)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LeaderIsEmpty);

            if (group.OtherOrgAmount.HasValue && group.OtherOrgAmount.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.OtherOrganizationAmountIsBadlyInformed);

            if (group.OtherOrgDebts.HasValue && group.OtherOrgDebts.Value == ERRORVALUE)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.OtherOrganizationDebtsIsBadlyInformed);

            if (group.BadClient && group.Comments == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.CommentsNeedFullIfBadClient);

            if (null == group.Branch)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.BranchIsEmpty);

            if (group.Id == 0 && IsGroupNameAlreadyUsedInDistrict(group.Name, group.District))
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NameAlreadyUsedInDistrict);
            
            if (group.Id != 0 && !group.Leader.CurrentlyIn)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LeaderIsEmpty);
            return status;
        }

        private bool IsGroupNameAlreadyUsedInDistrict(string pName, District pDistrict)
        {
            return _clientManagement.IsGroupNameAlreadyUsedInDistrict(pName, pDistrict);
        }

        private int AddGroup(Group group, Action<SqlTransaction, int> action)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            {
                group.Scoring = 0.6;
                try
                {
                    _clientManagement.AddNewGroup(group, sqlTransac);
                    if (action != null) action(sqlTransac, group.Id);
                    sqlTransac.Commit();
                    return group.Id;
                }
                catch (Exception ex)
                {
                    sqlTransac.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateGroup(Group group, Action<SqlTransaction, int> action)
        {
            IClient oldGroup = _clientManagement.SelectGroupById(group.Id);
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            {
                try
                {
                    _clientManagement.UpdateGroup(group, sqlTransac);
                    UpdateClientBranchHistory(group, oldGroup, sqlTransac);
                    if (action != null) action(sqlTransac, group.Id);
                    sqlTransac.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransac.Rollback();
                    throw ex;
                }
            }
        }

        public bool GuarantorIsNull(Guarantor guarantor)
        {
            bool result = false;
            if (guarantor == null)
                result = true;
            else if (guarantor.Tiers == null || guarantor.Amount == 0)
                result = true;

            return result;
        }

        public int SaveCorporate(Corporate client, FundingLine fundingLine, Action<SqlTransaction> action)
        {
            if (client.Id == 0)
                client.Id = AddBodyCorporate(client, fundingLine, action);
            else
                UpdateBodyCorporate(client, action);
            return client.Id;
        }

        public int AddBodyCorporate(Corporate body, FundingLine pFundingLine, Action<SqlTransaction> action)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            {
                try
                {
                    body.Type = OClientTypes.Corporate;
                    body.LoanCycle = 0;
                    body.Active = false;
                    body.BadClient = false;
                    ValidateCorporate(body);
                    body.Id = _clientManagement.AddBodyCorporate(body, pFundingLine, sqlTransac);
                    if (action != null) action(sqlTransac);
                    sqlTransac.Commit();
                    SavePicture(body);
                    SaveCorporatePerson(body.Id, body.Contacts);

                    return body.Id;
                }
                catch (Exception up)
                {
                    sqlTransac.Rollback();
                    throw up;
                }
            }
        }

        private void ValidateCorporate(Corporate body)
        {
            if (body.Activity == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.EconomicActivityIsNull);
            if (string.IsNullOrEmpty(body.Name))
                throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.NameIsEmpty);
            if (string.IsNullOrEmpty(body.City))
                throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.CityIsEmpty);
            if (body.District == null)
                throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.DistrictIsEmpty);
            if (null == body.Branch)
                throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.BranchIsEmpty);
        }

        public void UpdateBodyCorporate(Corporate body, Action<SqlTransaction> action)
        {
            IClient oldCorporate = _clientManagement.SelectClientById(body.Id);
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            {
                try
                {
                    if (body.Activity == null)
                        throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.EconomicActivityIsNull);
                    if (string.IsNullOrEmpty(body.Name))
                        throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.NameIsEmpty);
                    if (string.IsNullOrEmpty(body.City))
                        throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.CityIsEmpty);
                    if (body.District.Name == string.Empty || body.District == null)
                        throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.DistrictIsEmpty);
                    if (body.VolunteerCount == (int) EnumIHM.ErrorSaisieVolunteer)
                        throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.VolunteerIsFalseFormat);
                    if (body.EmployeeCount == (int) EnumIHM.ErrorSaisieEmployeeCount)
                        throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.EmployeeIsFalseFormat);

                    if (null == body.Branch)
                        throw new OpenCbsCorporateException(OpenCbsCorporateExceptionEnum.BranchIsEmpty);

                    _clientManagement.UpdateBodyCorporate(body, sqlTransac);
                    UpdateClientBranchHistory(body, oldCorporate, sqlTransac);
                    if (action != null) action(sqlTransac);
                    sqlTransac.Commit();

                    SaveCorporatePerson(body.Id, body.Contacts);
                    SavePicture(body);
                }
                catch (Exception ex)
                {
                    sqlTransac.Rollback();
                    throw ex;
                }
            }
        }

        public List<string> FindAllSetUpFields(string pOSetUpFieldTypes)
        {
            return _clientManagement.SelectAllSetUpFields(pOSetUpFieldTypes);
        }

        public int SaveNonSolidarityGroup(Village village)
        {
            return SaveNonSolidarityGroup(village, null);
        }

        public int SaveNonSolidarityGroup(Village village, Action<SqlTransaction, int> action)
        {
            ValidateVillage(village);

            if (0 == village.Id)
            {
                village.Id = AddVillage(village, action);
                UpdateVillage(village, action);
                SavePicture(village);
            }
            else
            {
                UpdateVillage(village, action);
                SavePicture(village);
            }

            return village.Id;
        }

        private void ValidateVillage(Village village)
        {
            if (null == village)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TiersIsNull);

            if (null == village.LoanOfficer)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.LoanOfficerIsEmpty);

            if (string.IsNullOrEmpty(village.Name))
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NameIsEmpty);

            if (village.District == null)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.DistrictIsNull);

            if (village.District.Id == 0)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.DistrictIsBad);

            if (_dataParam.IsCityMandatory)
            {
                if (village.City == null)
                    throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.CityIsNull);
            }

            if (null == village.Branch)
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.BranchIsEmpty);

            if (village.Members.Count < _dataParam.VillageMinMembers)
            {
                List<string> L = new List<string>();
                L.Add(_dataParam.VillageMinMembers.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.NoEnoughPersonsInThisGroup, L);
            }

            if (village.Members.Count > _dataParam.VillageMaxMembers)
            {
                List<string> L = new List<string>();
                L.Add(_dataParam.VillageMaxMembers.ToString());
                throw new OpenCbsTiersSaveException(OpenCbsTiersSaveExceptionEnum.TooMuchPersonsInThisGroup, L);
            }
        }

        private int AddVillage(Village village, Action<SqlTransaction, int> action)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            {
                try
                {
                    int retval = _clientManagement.AddVillage(village, sqlTransac);
                    if (action != null) action(sqlTransac, retval);

                    sqlTransac.Commit();
                    return retval;
                }
                catch (Exception)
                {
                    sqlTransac.Rollback();
                    throw;
                }
            }
        }

        private void UpdateVillage(Village village, Action<SqlTransaction, int> action)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction sqlTransac = connection.BeginTransaction())
            {
                try
                {
                    _clientManagement.UpdateVillage(village, sqlTransac);
                    if (action != null) action(sqlTransac, village.Id);
                    sqlTransac.Commit();
                }
                catch (Exception)
                {
                    sqlTransac.Rollback();
                    throw;
                }
            }
        }
        /// <summary>
        /// The method sets favourite loan officer to a village (non solidary corporate)
        /// </summary>
        /// <param name="village">an instance of type Village</param>
        public void SetFavouriteLoanOfficerForVillage(Village village)
        {
            using (SqlConnection connection = _clientManagement.GetConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
                try
                {
                    foreach (VillageMember member in village.Members)
                        _clientManagement.SetFavourLoanOfficerForPerson(member.Tiers.Id, village.LoanOfficer.Id, transaction);
                    transaction.Commit();
                } catch
                {
                    transaction.Rollback();
                    throw;
                }
        }

        /// <summary>
        /// The method sets favourite loan officer to a corporate (solidary corporate)
        /// </summary>
        /// <param name="group">an instance of type Village</param>
        /// <param name="loanOfficerId">loan officer id</param>
        /// <param name="transaction"> </param>
        public void SetFavouriteLoanOfficerForGroup(Group @group, int loanOfficerId, SqlTransaction transaction)
        {
            _clientManagement.SetFavourLoanOfficerForGroup(group.Id, loanOfficerId, transaction);
            foreach (Member member in group.Members)
                _clientManagement.SetFavourLoanOfficerForPerson(member.Tiers.Id, loanOfficerId, transaction);
        }

        /// <summary>
        /// The method sets favourite loan officer to a person (individual client)
        /// </summary>
        /// <param name="person">an instance of type Person</param>
        /// <param name="loanOfficerId">loan officer id</param>
        /// <param name="transaction"> </param>
        public void SetFavouriteLoanOfficerForPerson(Person person, int loanOfficerId, SqlTransaction transaction)
        {
            _clientManagement.SetFavourLoanOfficerForPerson(person.Id, loanOfficerId, transaction);
        }

        /// <summary>
        /// The method sets favourite loan officer to a corporate (individual client)
        /// </summary>
        /// <param name="corporate">an instance of type Corporate</param>
        ///  <param name="loanOfficerId">loan officer id</param>
        public void  SetFavouriteLoanOfficerForCorporate(Corporate corporate, int loanOfficerId)
        {
            _clientManagement.SetFavourLoanOfficerForCorporate(corporate.Id, loanOfficerId);
        }

        private void ClientSelected(IClient client)
        {
            if (null == client) return;
            BranchService bs = ServicesProvider.GetInstance().GetBranchService();
            client.Branch = bs.FindById(client.Branch.Id);
        }
        
        public void RemoveMember(Village village, VillageMember member)
        {
            if (null == village || null == member) return;
            village.DeleteMember(member);
        }

        public void AddExistingMember(Village village, VillageMember member)
        {
            if (null == village || null == member) return;
            village.AddMember(member);
        }

        public Person FindPersonByPassport(string passport)
        {
            return _clientManagement.SelectPersonByPassport(passport);
        }

        public Group FindGroupByName(string name)
        {
            return _clientManagement.SelectGroupByName(name);
        }

		public IClient FindClientByContractCode(string code)
        {
            int id = _clientManagement.SelectClientIdByContractCode(code);
            return _clientManagement.SelectClientById(id);
        }

        private void UpdateClientBranchHistory(IClient client, IClient oldClient, SqlTransaction sqlTransaction)
        {
            if (oldClient.Branch.Id != client.Branch.Id)
                _clientManagement.UpdateClientBranchHistory(client, oldClient, sqlTransaction);
        }
    }
}
