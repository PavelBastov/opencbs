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
using System.Data.SqlClient;
using OpenCBS.CoreDomain;
using System.Collections;
using OpenCBS.CoreDomain.Clients;
using OpenCBS.CoreDomain.Contracts.Loans;
using OpenCBS.CoreDomain.FundingLines;
using OpenCBS.CoreDomain.SearchResult;
using OpenCBS.Enums;
using OpenCBS.Manager.Contracts;
using OpenCBS.Shared;
using OpenCBS.Manager.QueryForObject;
using OpenCBS.Shared.Settings;

namespace OpenCBS.Manager.Clients
{
    /// <summary>
    /// ClientManagement contains all methods relative to selecting, inserting, updating
    /// and deleting person and group objects to and from our database.
    /// </summary>
    public class ClientManager : Manager
    {
        private readonly EconomicActivityManager _doam;
        private readonly LocationsManager _locations;
        private readonly ProjectManager _projectManager;
        private readonly SavingManager _savingManager;
        private readonly UserManager _userManager;

        public delegate void ClientSelectedEventHandler(IClient client);

        public event ClientManager.ClientSelectedEventHandler ClientSelected;

        public ClientManager(User pUser, bool pInitializeProject, bool pInitializeSavings) : base(pUser)
        {
            _doam = new EconomicActivityManager(pUser);
            _locations = new LocationsManager(pUser);
            if (pInitializeProject)
                _projectManager = new ProjectManager(pUser, true);
            if (pInitializeSavings)
                _savingManager = new SavingManager(pUser);
            _userManager = new UserManager(pUser);
        }

        public ClientManager(string pTestDb) : base(pTestDb)
        {
            _doam = new EconomicActivityManager(pTestDb);
            _locations = new LocationsManager(pTestDb);
            _projectManager = new ProjectManager(pTestDb);
            _savingManager = new SavingManager(pTestDb);
            _userManager = new UserManager(pTestDb);
        }

        public void UpdateTiers(IClient pTiers, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE [Tiers] SET  
                                        [scoring]=@scoring, 
                                        [active]=@active, 
                                        [loan_cycle] = @loanCycle, 
                                        [bad_client]=@badClient, 
                                        [other_org_name]=@otherOrgName, 
                                        [other_org_amount]=@otherOrgAmount, 
                                        [other_org_debts]=@otherOrgDebts, 
                                        [district_id]=@districtId, 
                                        [city]=@city, 
                                        [address]=@address, 
                                        [secondary_district_id]=@secondaryDistrictId, 
                                        [secondary_city]=@secondaryCity, 
                                        [secondary_address]=@secondaryAddress, 
                                        [cash_input_voucher_number]=@cashInput, 
                                        [cash_output_voucher_number]=@cashOutput,
                                        [home_phone] = @homePhone,
                                        [personal_phone]=@personalPhone,
                                        [secondary_home_phone]= @secondaryHomePhone,
                                        [secondary_personal_phone]=@secondaryPersonalPhone,
                                        [e_mail] = @email,
                                        [secondary_e_mail] = @secondaryEmail,
                                        [home_type] = @homeType, 
                                        [secondary_homeType] = @secondaryHomeType,
                                        [other_org_comment] = @otherOrgComment, 
                                        [sponsor1] = @sponsor1,
                                        [sponsor2] = @sponsor2,
                                        [sponsor1_comment] = @sponsor1Comment,
                                        [sponsor2_comment] = @sponsor2Comment,
                                        [follow_up_comment] = @followUpComment,
                                        [zipCode] = @zipCode, 
                                        [secondary_zipCode] = @secondaryZipCode
                                        , branch_id = @branch_id
                                WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@zipCode", pTiers.ZipCode);
                c.AddParam("@secondaryZipCode", pTiers.SecondaryZipCode);
                c.AddParam("@homeType", pTiers.HomeType);
                c.AddParam("@secondaryHomeType", pTiers.SecondaryHomeType);
                c.AddParam("@email", pTiers.Email);
                c.AddParam("@secondaryEmail", pTiers.SecondaryEmail);
                c.AddParam("@id", pTiers.Id);
                c.AddParam("@scoring", pTiers.Scoring);
                c.AddParam("@active", pTiers.Active);
                c.AddParam("@badClient", pTiers.BadClient);
                c.AddParam("@loanCycle", pTiers.LoanCycle);
                c.AddParam("@otherOrgName", pTiers.OtherOrgName);
                c.AddParam("@otherOrgAmount", pTiers.OtherOrgAmount);
                c.AddParam("@otherOrgDebts", pTiers.OtherOrgDebts);
                c.AddParam("@city", string.IsNullOrEmpty(pTiers.City)
                                                             ? pTiers.City
                                                             : pTiers.City.ToUpper());
                c.AddParam("@address", pTiers.Address);
                c.AddParam("@secondaryCity", string.IsNullOrEmpty(pTiers.SecondaryCity)
                                                             ? pTiers.SecondaryCity
                                                             : pTiers.SecondaryCity.ToUpper());
                c.AddParam("@secondaryAddress", pTiers.SecondaryAddress);

                c.AddParam("@homePhone", pTiers.HomePhone);
                c.AddParam("@personalPhone", pTiers.PersonalPhone);
                c.AddParam("@secondaryHomePhone", pTiers.SecondaryHomePhone);
                c.AddParam("@secondaryPersonalPhone", pTiers.SecondaryPersonalPhone);
                c.AddParam("@cashInput", pTiers.CashReceiptIn);
                c.AddParam("@cashOutput", pTiers.CashReceiptOut);
                c.AddParam("@otherOrgComment", pTiers.OtherOrgComment);
                c.AddParam("@sponsor1", pTiers.Sponsor1);
                c.AddParam("@sponsor2", pTiers.Sponsor2);
                c.AddParam("@sponsor1Comment", pTiers.Sponsor1Comment);
                c.AddParam("@sponsor2Comment", pTiers.Sponsor2Comment);
                c.AddParam("@followUpComment", pTiers.FollowUpComment);
                c.AddParam("@branch_id", pTiers.Branch.Id);

                if (pTiers.District != null)
                    c.AddParam("@districtId", pTiers.District.Id);
                else
                    c.AddParam("@districtId", null);

                if (pTiers.SecondaryDistrict != null)
                    c.AddParam("@secondaryDistrictId", pTiers.SecondaryDistrict.Id);
                else
                    c.AddParam("@secondaryDistrictId", null);

                c.ExecuteNonQuery();
            }
        }

        public int AddPerson(Person person)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                int id = AddPerson(person, transaction);
                transaction.Commit();
                return id;
            }
        }

        public int AddPerson(Person pPerson, SqlTransaction pSqlTransac)
        {
            int tiersId = AddTiers(pPerson, pSqlTransac);

            const string q = @"INSERT INTO [Persons]
                            ([id], 
                            [first_name], 
                            [sex], 
                            [identification_data], 
                            [last_name], 
                            [birth_date], 
                            [household_head], 
                            [nb_of_dependents], 
                            [nb_of_children], 
                            [children_basic_education], 
                            [livestock_number], 
                            [livestock_type], 
                            [landplot_size], 
                            [home_size], 
                            [home_time_living_in], 
                            [capital_other_equipments], 
                            [activity_id], 
                            [experience], 
                            [nb_of_people],
                            [father_name],
                            [mother_name],
                            [image_path],
                            [birth_place],
                            [nationality],
                            [study_level],
                            [SS],
                            [CAF],
                            [housing_situation],
                            [handicapped],
                            [professional_situation],
                            [professional_experience],
                            [first_contact],
                            [first_appointment],
                            [family_situation],
                            [povertylevel_childreneducation],
                            [povertylevel_economiceducation],
                            [povertylevel_socialparticipation],
                            [povertylevel_healthsituation],
                            [unemployment_months]
                            ) 
                VALUES(
                            @id
                            ,@firstName
                            ,@sex
                            ,@identificationData
                            ,@lastName
                            ,@birthDate
                            ,@householdHead
                            ,@nbOfDependents
                            ,@nbOfChildren
                            ,@childrenBasicEducation
                            ,@livestockNumber
                            ,@livestockType
                            ,@landplotSize
                            ,@homeSize
                            ,@homeTimeLivingIn
                            ,@capitalOtherEquipments
                            ,@activityId
                            ,@experience
                            ,@nbOfPeople
                            ,@fatherName
                            ,@motherName
                            ,@imagePath
                            ,@BirthPlace
                            ,@Nationality
                            ,@StudyLevel
                            ,@SS
                            ,@CAF
                            ,@HousingSituation
                            ,@handicapped
                            ,@professionalSituation
                            ,@professionalExperience
                            ,@firstContact
                            ,@firstAppointment
                            ,@family_situation
                            ,@povertyLevelChildrenEducation
                            ,@povertyLevelEconomicEducation
                            ,@povertyLevelSocialParticipation
                            ,@povertyLevelHealthSituation
                            ,@unemploymentMonths
                        )";


            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                
                c.AddParam("@handicapped", pPerson.Handicapped);
                c.AddParam("@BirthPlace", pPerson.BirthPlace);
                c.AddParam("@firstContact", pPerson.FirstContact);
                c.AddParam("@firstAppointment", pPerson.FirstAppointment);
                c.AddParam("@professionalExperience", pPerson.ProfessionalExperience);
                c.AddParam("@professionalSituation", pPerson.ProfessionalSituation);
                c.AddParam("@Nationality", pPerson.Nationality);
                c.AddParam("@unemploymentMonths", pPerson.UnemploymentMonths);
                c.AddParam("@SS", pPerson.SSNumber);
                c.AddParam("@StudyLevel", pPerson.StudyLevel);
                c.AddParam("@CAF", pPerson.CAFNumber);
                c.AddParam("@HousingSituation", pPerson.HousingSituation);

                c.AddParam("@id", tiersId);
                c.AddParam("@povertyLevelChildrenEducation", pPerson.PovertyLevelIndicators.ChildrenEducation);
                c.AddParam("@povertyLevelHealthSituation", pPerson.PovertyLevelIndicators.HealthSituation);
                c.AddParam("@povertyLevelSocialParticipation", pPerson.PovertyLevelIndicators.SocialParticipation);
                c.AddParam("@povertyLevelEconomicEducation", pPerson.PovertyLevelIndicators.EconomicEducation);

                c.AddParam("@firstName", pPerson.FirstName);
                c.AddParam("@sex", pPerson.Sex);
                c.AddParam("@identificationData", pPerson.IdentificationData);
                c.AddParam("@lastName", pPerson.LastName);
                c.AddParam("@birthDate", pPerson.DateOfBirth);
                c.AddParam("@householdHead", pPerson.HouseHoldHead);
                c.AddParam("@nbOfDependents", pPerson.NbOfDependents);
                c.AddParam("@nbOfChildren", pPerson.NbOfChildren);
                c.AddParam("@childrenBasicEducation", pPerson.ChildrenBasicEducation);
                c.AddParam("@livestockNumber", pPerson.LivestockNumber);
                c.AddParam("@livestockType", pPerson.LivestockType);
                c.AddParam("@landPlotSize", pPerson.LandplotSize);
                c.AddParam("@homeSize", pPerson.HomeSize);
                c.AddParam("@homeTimeLivingIn", pPerson.HomeTimeLivingIn);
                c.AddParam("@capitalOtherEquipments", pPerson.CapitalOthersEquipments);
                c.AddParam("@experience", pPerson.Experience);
                c.AddParam("@nbOfPeople", pPerson.NbOfPeople);
                c.AddParam("@fatherName", pPerson.FatherName);
                c.AddParam("@motherName", pPerson.MotherName);
                c.AddParam("@imagePath", pPerson.Image);
                c.AddParam("@family_situation", pPerson.FamilySituation);

                if (pPerson.Activity != null)
                    c.AddParam("@activityId", pPerson.Activity.Id);
                else
                    c.AddParam("@activityId", null);

                c.ExecuteNonQuery();
            }
            pPerson.Id = tiersId;

            return tiersId;
        }

        public void UpdatePersonIdentificationData(int pPersonId, SqlTransaction sqlTransac)
        {
            string q =
                @"UPDATE Persons SET identification_data = @identificationData
                               WHERE id = @id";
            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@id", pPersonId);
                c.AddParam("@identificationData", pPersonId.ToString());
                c.ExecuteScalar();
            }
        }

        public Person SelectPersonById(int pPersonId)
        {
            Person person = null;
            int? districtId = null;
            int? secondaryDistrictId = null;
            int? activityId = null;
            int? personalBankId = null;
            int? businessBankId = null;

            string q = @"SELECT 
                                 Tiers.id AS tiers_id, 
                                 Tiers.client_type_code, 
                                 Tiers.scoring, 
                                 Tiers.loan_cycle,
                                 Tiers.active, 
                                 Tiers.bad_client, 
                                 Tiers.other_org_name, 
                                 Tiers.other_org_amount, 
                                 Tiers.other_org_debts, 
                                 Tiers.district_id, 
                                 Tiers.city, 
                                 Tiers.address, 
                                 Tiers.secondary_district_id, 
                                 Tiers.secondary_city, 
                                 Tiers.secondary_address, 
                                 Tiers.cash_input_voucher_number, 
                                 Tiers.cash_output_voucher_number,
                                 Tiers.status,
                                 Tiers.home_phone, 
                                 Tiers.personal_phone,
                                 Tiers.secondary_home_phone,
                                 Tiers.secondary_personal_phone,
                                 Tiers.e_mail,
                                 Tiers.secondary_e_mail,
                                 Tiers.home_type,
                                 Tiers.zipCode, 
                                 Tiers.secondary_zipCode,
                                 Tiers.secondary_homeType,
                                 Tiers.other_org_comment,
                                 Tiers.sponsor1,Tiers.sponsor2,
                                 Tiers.sponsor1_comment,Tiers.sponsor2_comment,
                                 Tiers.follow_up_comment,
                                 Tiers.branch_id,
                                 Persons.first_name, 
                                 Persons.sex, 
                                 Persons.identification_data, 
                                 Persons.last_name, 
                                 Persons.birth_date, 
                                 Persons.household_head, 
                                 Persons.nb_of_dependents, 
                                 Persons.nb_of_children, 
                                 Persons.children_basic_education, 
                                 Persons.livestock_number, 
                                 Persons.livestock_type, 
                                 Persons.landplot_size, 
                                 Persons.home_time_living_in, 
                                 Persons.home_size, 
                                 Persons.capital_other_equipments, 
                                 Persons.activity_id, 
                                 Persons.experience, 
                                 Persons.nb_of_people, 
                                 Persons.mother_name, 
                                 Persons.image_path, 
                                 Persons.father_name, 
                                 Persons.birth_place, 
                                 Persons.nationality,
                                 Persons.unemployment_months,
                                 Persons.personalBank_id,
                                 Persons.businessBank_id, 
                                 Persons.study_level, 
                                 Persons.SS, 
                                 Persons.CAF, 
                                 Persons.housing_situation, 
                                 Persons.handicapped,
                                 Persons.professional_situation, 
                                 Persons.professional_experience,
                                 Persons.first_contact,
                                 Persons.first_appointment, 
                                 Persons.family_situation,
                                 Persons.povertylevel_childreneducation,
                                 Persons.povertylevel_economiceducation,
                                 Persons.povertylevel_socialparticipation,
                                 Persons.povertylevel_healthsituation,
                                 Persons.loan_officer_id      
                            FROM Tiers 
                            INNER JOIN Persons ON Tiers.id = Persons.id 
                            WHERE Persons.id = @id";

            
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pPersonId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        r.Read();
                        person = GetPersonFromReader(r);

                        personalBankId = r.GetNullInt("personalBank_id");
                        businessBankId = r.GetNullInt("businessBank_id");
                        secondaryDistrictId = r.GetNullInt("secondary_district_id");
                        activityId = r.GetNullInt("activity_id");
                        districtId = r.GetNullInt("district_id");
                    }
                }
            }
            if (person != null)
            {
                UserManager userManager = new UserManager(User.CurrentUser);
                if (person.FavouriteLoanOfficerId.HasValue)
                    person.FavouriteLoanOfficer = userManager.SelectUser((int) person.FavouriteLoanOfficerId,
                                                                         true);

                if (activityId.HasValue)
                    person.Activity = _doam.SelectEconomicActivity(activityId.Value);

                if (districtId.HasValue)
                    person.District = _locations.SelectDistrictById(districtId.Value);

                if (secondaryDistrictId.HasValue)
                    person.SecondaryDistrict = _locations.SelectDistrictById(secondaryDistrictId.Value);

                if (person != null)
                {
                    if (_projectManager != null)
                        person.AddProjects(_projectManager.SelectProjectsByClientId(person.Id));
                    if (_savingManager != null)
                        person.AddSavings(_savingManager.SelectSavings(person.Id));
                }

                OnClientSelected(person);
            }
            return person;
        }

        private Person GetPersonFromReader(OpenCbsReader r)
        {
            Person person;
            person = new Person
                         {
                             Id = r.GetInt("tiers_id"),
                             HomePhone = r.GetString("home_phone"),
                             FirstContact = r.GetNullDateTime("first_contact"),
                             FirstAppointment = r.GetNullDateTime("first_appointment"),
                             ProfessionalSituation = r.GetString("professional_situation"),
                             ProfessionalExperience = r.GetString("professional_experience"),
                             FamilySituation = r.GetString("family_situation"),
                             Handicapped = r.GetBool("handicapped"),
                             Email = r.GetString("e_mail"),
                             Status = (OClientStatus) r.GetSmallInt("status"),
                             SecondaryEmail = r.GetString("secondary_e_mail"),
                             HomeType = r.GetString("home_type"),
                             SecondaryHomeType = r.GetString("secondary_hometype"),
                             ZipCode = r.GetString("zipCode"),
                             SecondaryZipCode = r.GetString("secondary_zipCode"),
                             OtherOrgComment = r.GetString("other_org_comment"),
                             PersonalPhone = r.GetString("personal_phone"),
                             SecondaryHomePhone = r.GetString("secondary_home_phone"),
                             SecondaryPersonalPhone = r.GetString("secondary_personal_phone"),
                             CashReceiptIn = r.GetNullInt("cash_input_voucher_number"),
                             CashReceiptOut = r.GetNullInt("cash_output_voucher_number"),
                             Type = r.GetChar("client_type_code") == 'I'
                                        ? OClientTypes.Person
                                        : r.GetChar("client_type_code") == 'G'
                                              ? OClientTypes.Group
                                              : OClientTypes.Corporate,
                             Scoring = r.GetNullDouble("scoring"),
                             LoanCycle = r.GetInt("loan_cycle"),
                             Active = r.GetBool("active"),
                             BadClient = r.GetBool("bad_client"),
                             OtherOrgName = r.GetString("other_org_name"),
                             OtherOrgAmount = r.GetMoney("other_org_amount"),
                             OtherOrgDebts = r.GetMoney("other_org_debts"),
                             City = r.GetString("city"),
                             Address = r.GetString("address"),
                             SecondaryCity = r.GetString("secondary_city"),
                             SecondaryAddress = r.GetString("secondary_address"),
                             FirstName = r.GetString("first_name"),
                             Sex = r.GetChar("sex"),
                             IdentificationData = r.GetString("identification_data"),
                             DateOfBirth = r.GetNullDateTime("birth_date"),
                             LastName = r.GetString("last_name"),
                             HouseHoldHead = r.GetBool("household_head"),
                             NbOfDependents = r.GetNullInt("nb_of_dependents"),
                             NbOfChildren = r.GetNullInt("nb_of_children"),
                             ChildrenBasicEducation = r.GetNullInt("children_basic_education"),
                             LivestockNumber = r.GetNullInt("livestock_number"),
                             LivestockType = r.GetString("livestock_type"),
                             LandplotSize = r.GetNullDouble("landplot_size"),
                             HomeTimeLivingIn = r.GetNullInt("home_time_living_in"),
                             HomeSize = r.GetNullDouble("home_size"),
                             CapitalOthersEquipments = r.GetString("capital_other_equipments"),
                             Experience = r.GetNullInt("experience"),
                             NbOfPeople = r.GetNullInt("nb_of_people"),
                             FatherName = r.GetString("father_name"),
                             MotherName = r.GetString("mother_name"),
                             Image = r.GetString("image_path"),
                             StudyLevel = r.GetString("study_level"),
                             BirthPlace = r.GetString("birth_place"),
                             Nationality = r.GetString("nationality"),
                             UnemploymentMonths = r.GetNullInt("unemployment_months"),
                             SSNumber = r.GetString("SS"),
                             CAFNumber = r.GetString("CAF"),
                             HousingSituation = r.GetString("housing_situation"),
                             FollowUpComment = r.GetString("follow_up_comment"),
                             Sponsor1 = r.GetString("sponsor1"),
                             Sponsor2 = r.GetString("sponsor2"),
                             Sponsor1Comment = r.GetString("sponsor1_comment"),
                             Sponsor2Comment = r.GetString("sponsor2_comment"),
                             FavouriteLoanOfficerId = r.GetNullInt("loan_officer_id"),
                             Branch = new Branch {Id = r.GetInt("branch_id")},

                             PovertyLevelIndicators =
                                 {
                                     ChildrenEducation = r.GetInt("povertylevel_childreneducation"),
                                     EconomicEducation = r.GetInt("povertylevel_economiceducation"),
                                     HealthSituation = r.GetInt("povertylevel_socialparticipation"),
                                     SocialParticipation = r.GetInt("povertylevel_healthsituation")
                                 }
                         };
            return person;
        }

        public Person SelectPersonByName(string name)
        {
            const string q = @"SELECT id FROM dbo.Persons WHERE first_name + ' ' + last_name = @name";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@name", name);
                int id;
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r.Empty) return null;
                    r.Read();
                    id = r.GetInt("id");
                }
                return SelectPersonById(id);
            }
        }

        public void UpdatePerson(Person person)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                UpdatePerson(person, transaction);
                transaction.Commit();
            }
        }

        public void UpdateClientBranchHistory(IClient client, IClient oldClient, SqlTransaction sqlTransaction)
        {
            const string q =
                @"INSERT INTO [ClientBranchHistory] (date_changed, branch_from_id, branch_to_id, client_id, user_id)
            VALUES(@date, @branch_from_id, @branch_to_id, @client_id, @user_id)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransaction.Connection, sqlTransaction))
            {
                c.AddParam("@date", TimeProvider.Today);
                c.AddParam("@branch_from_id", oldClient.Branch.Id);
                c.AddParam("@branch_to_id", client.Branch.Id);
                c.AddParam("@client_id", client.Id);
                c.AddParam("@user_id", User.CurrentUser.Id);
                c.ExecuteNonQuery();
            }
        }


        public void UpdatePerson(Person person, SqlTransaction sqlTransac)
        {
            UpdateTiers(person, sqlTransac);

            const string q = @"UPDATE [Persons] SET 
                                [first_name]=@firstName
                                ,[sex]=@sex
                                ,[identification_data]=@identificationData
                                ,[last_name]=@lastName
                                ,[birth_date]=@birthDate
                                ,[household_head]=@householdHead
                                ,[nb_of_dependents]=@nbOfDependents
                                ,[nb_of_children]=@nbOfChildren
                                ,[children_basic_education]=@childrenBasicEducation
                                ,[livestock_number]=@livestockNumber
                                ,[livestock_type]=@livestockType
                                ,[landplot_size]=@landPlotSize
                                ,[home_size]=@homeSize
                                ,[home_time_living_in]=@homeTimeLivingIn
                                ,[capital_other_equipments]=@capitalOtherEquipments
                                ,[activity_id]=@activityId
                                ,[experience]=@experience
                                ,[nb_of_people]=@nbOfPeople
                                ,[father_name]=@fathername
                                ,[mother_name]=@mothername
                                ,[image_path]=@image
                                ,[study_level] = @studyLevel
                                ,[birth_place] = @birthPlace
                                ,[nationality] = @nationality
                                ,[SS] = @SS
                                ,[CAF] = @CAF
                                ,[housing_situation] = @housing_situation
                                ,[unemployment_months] = @UnemploymentMonths
                                ,[handicapped] = @handicapped
                                ,[professional_situation] = @ProfessionalSituation
                                ,[professional_experience] = @ProfessionalExperience
                                ,[first_contact] = @firstContact
                                ,[first_appointment] = @firstAppointment
                                ,[family_situation] = @family_situation
                                ,[povertylevel_healthsituation]=@povertyLevelHealthSituation
                                ,[povertylevel_childreneducation]=@povertyLevelChildrenEducation
                                ,[povertylevel_socialparticipation]=@povertyLevelSocialParticipation
                                ,[povertylevel_economiceducation]= @povertyLevelEconomicEducation   
                                WHERE id = @id";


            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {

                var formatInfo = new NameFormatInfo();
                var settings = ApplicationSettings.GetInstance("");
                var fnFormat = @"{0:" + settings.FirstNameFormat + @"}";
                var lnFormat = @"{0:" + settings.LastNameFormat + @"}";
                person.FirstName = string.Format(formatInfo, fnFormat, person.FirstName);
                person.LastName = string.Format(formatInfo, lnFormat, person.LastName);

                c.AddParam("@handicapped", person.Handicapped);
                c.AddParam("@studyLevel", person.StudyLevel);
                c.AddParam("@firstContact", person.FirstContact);
                c.AddParam("@firstAppointment", person.FirstAppointment);
                c.AddParam("@ProfessionalSituation", person.ProfessionalSituation);
                c.AddParam("@ProfessionalExperience", person.ProfessionalExperience);
                c.AddParam("@birthPlace", person.BirthPlace);
                c.AddParam("@nationality", person.Nationality);
                c.AddParam("@UnemploymentMonths", person.UnemploymentMonths);
                c.AddParam("@SS", person.SSNumber);
                c.AddParam("@CAF", person.CAFNumber);
                c.AddParam("@housing_situation", person.HousingSituation);
                c.AddParam("@id", person.Id);
                c.AddParam("@firstName", person.FirstName);
                c.AddParam("@sex", person.Sex);
                c.AddParam("@identificationData", person.IdentificationData);
                c.AddParam("@lastName", person.LastName);
                c.AddParam("@birthDate", person.DateOfBirth);
                c.AddParam("@householdHead", person.HouseHoldHead);
                c.AddParam("@nbOfDependents", person.NbOfDependents);
                c.AddParam("@nbOfChildren", person.NbOfChildren);
                c.AddParam("@childrenBasicEducation", person.ChildrenBasicEducation);
                c.AddParam("@livestockNumber", person.LivestockNumber);
                c.AddParam("@livestockType", person.LivestockType);
                c.AddParam("@landPlotSize", person.LandplotSize);
                c.AddParam("@homeSize", person.HomeSize);
                c.AddParam("@homeTimeLivingIn", person.HomeTimeLivingIn);
                c.AddParam("@capitalOtherEquipments", person.CapitalOthersEquipments);
                c.AddParam("@experience", person.Experience);
                c.AddParam("@nbOfPeople", person.NbOfPeople);
                c.AddParam("@fathername", person.FatherName);
                c.AddParam("@mothername", person.MotherName);
                c.AddParam("@image", person.Image);
                c.AddParam("@family_situation",  person.FamilySituation);
                c.AddParam("@povertyLevelChildrenEducation", person.PovertyLevelIndicators.ChildrenEducation);
                c.AddParam("@povertyLevelHealthSituation", person.PovertyLevelIndicators.HealthSituation);
                c.AddParam("@povertyLevelSocialParticipation", person.PovertyLevelIndicators.SocialParticipation);
                c.AddParam("@povertyLevelEconomicEducation", person.PovertyLevelIndicators.EconomicEducation);

                if (person.Activity != null)
                    c.AddParam("@activityId", person.Activity.Id);
                else
                    c.AddParam("@activityId", null);

                c.ExecuteNonQuery();
            }
        }

       private int? SelectCurrentlyGroupIdForAPersonId(int personId, SqlTransaction transac)
        {
            int? result = null;
            string q = @"SELECT 
                                 [person_id], 
                                 [group_id], 
                                 [is_leader], 
                                 [currently_in] 
                               FROM [PersonGroupBelonging] 
                               WHERE person_id = @id AND currently_in = 1";

            using (OpenCbsCommand c = new OpenCbsCommand(q, transac.Connection, transac))
            {
                c.AddParam("@id", personId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    r.Read();
                    if (!r.Empty)
                    {
                        result = r.GetNullInt("group_id");
                    }
                }
            }
            return result;
        }

        public bool IsLeader(int personId, int groupId)
        {
            bool status = false;
            string q = @"SELECT [is_leader] 
                            FROM [PersonGroupBelonging] 
                            INNER JOIN dbo.Groups ON dbo.PersonGroupBelonging.group_id = dbo.Groups.id 
                            INNER JOIN dbo.Tiers ON dbo.Groups.id = dbo.Tiers.id 
                            WHERE person_id = @id AND group_id = @group_id
                              AND currently_in = 1";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", personId);
                c.AddParam("@group_id", groupId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    r.Read();
                    if (!r.Empty)
                    {
                        status = r.GetBool("is_leader");
                    }
                }
                return status;
            }
        }

        public bool IsLeaderInVillage(int personId)
        {
            bool status = false;
            string q = @"SELECT [is_leader]
                            FROM [VillagesPersons]
                            INNER JOIN dbo.Villages ON dbo.VillagesPersons.village_id = dbo.Villages.id
                            INNER JOIN dbo.Tiers ON dbo.Villages.id = dbo.Tiers.id
                            WHERE person_id = @id
                              AND currently_in = 1";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", personId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    r.Read();
                    if (!r.Empty)
                    {
                        status = r.GetBool("is_leader");
                    }
                }
                return status;
            }
        }

        public bool IsLeaderIsActive(int personId)
        {
            bool status = false;
            string q = @"SELECT [is_leader] 
                            FROM [PersonGroupBelonging] 
                            INNER JOIN dbo.Groups ON dbo.PersonGroupBelonging.group_id = dbo.Groups.id 
                            INNER JOIN dbo.Tiers ON dbo.Groups.id = dbo.Tiers.id 
                            WHERE person_id = @id 
                              AND currently_in = 1
                              AND Tiers.active = 1";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", personId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    r.Read();
                    if (!r.Empty)
                    {
                        status = r.GetBool("is_leader");
                    }
                }
                return status;
            }
        }

        public int AddNewGroup(Group group)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                int id = AddNewGroup(group, transaction);
                transaction.Commit();
                return id;
            }
        }

        public int AddNewGroup(Group group, SqlTransaction sqlTransac)
        {
            int tiersId = AddTiers(group, sqlTransac);

            string q = @"INSERT INTO [Groups]
                            (
                                [id], 
                                [name], 
                                [establishment_date], 
                                [comments], 
                                [meeting_day]
                            ) 
                            VALUES 
                            (
                                @id, 
                                @name, 
                                @establishmentDate, 
                                @comments, 
                                @meeting_day
                            )";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@id", tiersId);
                c.AddParam("@name", group.Name);
                c.AddParam("@establishmentDate", group.EstablishmentDate);
                c.AddParam("@comments", group.Comments);
                if (group.MeetingDay.HasValue)
                {
                    c.AddParam("@meeting_day", (int) group.MeetingDay);
                }
                else
                {
                    c.AddParam("@meeting_day", null);
                }
                group.Id = tiersId;
                c.ExecuteNonQuery();
            }

            foreach (Member member in group.Members)
            {
                int? groupId = SelectCurrentlyGroupIdForAPersonId(member.Tiers.Id, sqlTransac);

                //if (groupId.HasValue)
                //    UpdatePersonFromGroup(member.Tiers.Id, groupId.Value, sqlTransac);

                bool leader = (member).Equals(@group.Leader);
                AddMemberToGroup(member, group, leader, sqlTransac);
                UpdatePerson((Person) member.Tiers, sqlTransac);
            }
        
            return tiersId;
        }

        private void AddMemberToGroup(Member pMember, Group group, bool leader, SqlTransaction sqlTransac)
        {
            string q = @"INSERT INTO [PersonGroupBelonging]
                                    (
                                         [person_id]
                                        ,[group_id]
                                        ,[is_leader]
                                        ,[currently_in]
                                        ,[joined_date]
                                        ,[left_date]
                                    )
                                VALUES
                                    (
                                        @personId
                                        ,@groupId
                                        ,@isLeader
                                        ,@currentlyIn
                                        ,@joinedDate
                                        ,@leftDate
                                    )";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@personId", pMember.Tiers.Id);
                c.AddParam("@groupId", group.Id);
                c.AddParam("@isLeader", leader);
                c.AddParam("@currentlyIn", true);
                c.AddParam("@joinedDate", TimeProvider.Today);
                c.AddParam("@leftDate", null);

                c.ExecuteNonQuery();
            }
        }

        private void UpdatePersonToGroup(Member pMember, int groupId, bool leader, SqlTransaction sqlTransac)
        {
            string q = @"UPDATE [PersonGroupBelonging] 
                               SET [is_leader]= @isLeader,
                                 [currently_in]= @currentlyIn,
                                 [left_date] = @leftDate
                               WHERE [person_id]= @personId
                               AND [group_id]= @groupId";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@personId", pMember.Tiers.Id);
                c.AddParam("@groupId", groupId);
                c.AddParam("@isLeader", leader);
                c.AddParam("@leftDate", null);
                c.AddParam("@currentlyIn", true);

                c.ExecuteNonQuery();
            }
        }

        public void UpdatePersonFromGroup(int personId, int groupId, SqlTransaction sqlTransac)
        {
            string q = @"UPDATE [PersonGroupBelonging] 
                               SET currently_in = 0, 
                                   left_date = @leftDate 
                               WHERE group_id = @groupId 
                               AND person_id = @personId";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@groupId", groupId);
                c.AddParam("@personId", personId);
                c.AddParam("@leftDate", TimeProvider.Today);

                c.ExecuteNonQuery();
            }

            q = @"UPDATE [Tiers] SET active = 0 WHERE id = @personId";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@personId", personId);
                c.ExecuteNonQuery();
            }
        }

        private void DeleteVillageMember(int village_id, int person_id, SqlTransaction t)
        {
            const string q = @"UPDATE VillagesPersons 
                                SET left_date = @left_date,
                                    currently_in = 0
                                WHERE village_id = @village_id 
                                AND person_id = @person_id";
            
            using (OpenCbsCommand c = new OpenCbsCommand(q, t.Connection, t))
            {
                c.AddParam("@village_id", village_id);
                c.AddParam("@person_id", person_id);
                c.AddParam("@left_date", TimeProvider.Now);
                c.ExecuteNonQuery();
            }
        }

        public List<Village> SelectAllNSGsWhereSelectedPersonIsAMember(int pPersonId)
        {
            List<Village> list = new List<Village>();
            const string q = @"SELECT Villages.id 
                                      FROM Villages, VillagesPersons 
                                      WHERE Villages.id = VillagesPersons.village_id
                                      AND VillagesPersons.person_id = @personId";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@personId", pPersonId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            Village village = new Village();
                            village.Id = r.GetInt("id");
                            list.Add(village);
                        }
                    }
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = SelectVillageById(list[i].Id);
            }
            return list;
        }

        public List<Group> SelectAllGroupsWhereSelectedPersonIsAMember(int pPersonId)
        {
            List<Group> list = new List<Group>();
            const string q = @"SELECT Groups.id 
                                    FROM Groups,PersonGroupBelonging 
                                    WHERE Groups.id = PersonGroupBelonging.group_id 
                                    AND PersonGroupBelonging.person_id = @personId
                                    AND PersonGroupBelonging.left_date IS NULL";

            using (SqlConnection conn = GetConnection())
            {
                using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
                {
                    c.AddParam("@personId", pPersonId);
                    using (OpenCbsReader r = c.ExecuteReader())
                    {
                        if (!r.Empty)
                        {
                            while (r.Read())
                            {
                                Group group = new Group();
                                group.Id = r.GetInt("id");
                                list.Add(group);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = SelectGroupById(list[i].Id);
            }
            return list;
        }

        private Group SelectGroup(int pGroupId)
        {
            Group group = null;
            int? districtId = null;
            int? secondaryDistrictId = null;
            int? activityId = null;

            const string q = @"SELECT 
                Tiers.id AS tiers_id, 
                Tiers.client_type_code, 
                Tiers.scoring, 
                Tiers.loan_cycle, 
                Tiers.active,
                Tiers.bad_client, 
                Tiers.other_org_name, 
                Tiers.other_org_amount, 
                Tiers.other_org_debts, 
                Tiers.district_id, 
                Tiers.city, 
                Tiers.address, 
                Tiers.secondary_district_id, 
                Tiers.secondary_city, 
                Tiers.secondary_address,
                Tiers.status, 
                Tiers.home_phone,
                Tiers.personal_phone,
                Tiers.secondary_home_phone,
                Tiers.secondary_personal_phone,
                Tiers.cash_input_voucher_number, 
                Tiers.cash_output_voucher_number,
                Tiers.e_mail,
                Tiers.secondary_e_mail,
                Tiers.home_type,
                Tiers.zipCode,
                Tiers.secondary_zipCode,
                Tiers.secondary_homeType, 
                Tiers.other_org_comment,
                Tiers.sponsor1,
                Tiers.sponsor2,
                Tiers.sponsor1_comment,
                Tiers.sponsor2_comment,
                Tiers.follow_up_comment,
                Tiers.branch_id,
                Groups.name, 
                Groups.establishment_date, 
                Groups.comments,
                Groups.meeting_day,
                eah.economic_activity_id,
                Groups.loan_officer_id 
                FROM Tiers 
                INNER JOIN Groups ON Tiers.id = Groups.id
                LEFT JOIN EconomicActivityLoanHistory eah ON eah.group_id = Groups.id 
                WHERE Groups.id = @id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {

                c.AddParam("@id", pGroupId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null && !r.Empty)
                    {
                        r.Read();
                        group = new Group
                                    {
                                        Id = r.GetInt("tiers_id"),
                                        HomeType = r.GetString("home_type"),
                                        SecondaryHomeType = r.GetString("secondary_hometype"),
                                        ZipCode = r.GetString("zipCode"),
                                        SecondaryZipCode = r.GetString("secondary_zipCode"),
                                        HomePhone = r.GetString("home_phone"),
                                        PersonalPhone = r.GetString("personal_phone"),
                                        SecondaryHomePhone = r.GetString("secondary_home_phone"),
                                        SecondaryPersonalPhone = r.GetString("secondary_personal_phone"),
                                        Status = ((OClientStatus) r.GetSmallInt("status")),
                                        CashReceiptIn = r.GetNullInt("cash_input_voucher_number"),
                                        CashReceiptOut = r.GetNullInt("cash_output_voucher_number"),
                                        Type = r.GetChar("client_type_code") == 'I'
                                                   ? OClientTypes.Person
                                                   : r.GetChar("client_type_code") ==
                                                     'G'
                                                         ? OClientTypes.Group
                                                         : OClientTypes.Corporate,
                                        Scoring = r.GetNullDouble("scoring"),
                                        LoanCycle = r.GetInt("loan_cycle"),
                                        Active = r.GetBool("active"),
                                        BadClient = r.GetBool("bad_client"),
                                        OtherOrgName = r.GetString("other_org_name"),
                                        OtherOrgAmount = r.GetMoney("other_org_amount"),
                                        OtherOrgDebts = r.GetMoney("other_org_debts"),
                                        OtherOrgComment = r.GetString("other_org_comment"),
                                        Comments = r.GetString("comments"),
                                        Email = r.GetString("e_mail"),
                                        SecondaryEmail = r.GetString("secondary_e_mail"),
                                        MeetingDay = (DayOfWeek?) r.GetNullInt("meeting_day"),
                                        Branch = new Branch {Id = r.GetInt("branch_id")},
                                        FavouriteLoanOfficerId = r.GetNullInt("loan_officer_id")
                                    };

                        districtId = r.GetNullInt("district_id");

                        group.City = r.GetString("city");
                        group.Address = r.GetString("address");
                        secondaryDistrictId = r.GetNullInt("secondary_district_id");
                        group.SecondaryCity = r.GetString("secondary_city");
                        group.SecondaryAddress = r.GetString("secondary_address");
                        group.Name = r.GetString("name");
                        group.EstablishmentDate = r.GetNullDateTime("establishment_date");
                        group.FollowUpComment = r.GetString("follow_up_comment");
                        group.Sponsor1 = r.GetString("sponsor1");
                        group.Sponsor2 = r.GetString("sponsor2");
                        group.Sponsor1Comment = r.GetString("sponsor1_comment");
                        group.Sponsor2Comment = r.GetString("sponsor2_comment");

                        activityId = r.GetNullInt("economic_activity_id");

                        UserManager userManager = new UserManager(User.CurrentUser);
                        if (group.FavouriteLoanOfficerId.HasValue)
                            group.FavouriteLoanOfficer = userManager.SelectUser((int) group.FavouriteLoanOfficerId,
                                                                                true);
                    }
                }
            }

            if (districtId.HasValue)
                group.District = _locations.SelectDistrictById(districtId.Value);

            if (secondaryDistrictId.HasValue)
                group.SecondaryDistrict = _locations.SelectDistrictById(secondaryDistrictId.Value);

            if (group != null)
            {
                if (_projectManager != null)
                    group.AddProjects(_projectManager.SelectProjectsByClientId(group.Id));
                List<Member> idsList = SelectPersonIdsByGroupId(group.Id, true);

                if (_savingManager != null)
                    group.AddSavings(_savingManager.SelectSavings(group.Id));

                foreach (Member member in idsList)
                {
                    member.Tiers = SelectPersonById(member.Tiers.Id);
                    if (IsLeader(member.Tiers.Id, group.Id))
                        group.Leader = member;

                    else
                        group.AddMember(member);
                }

                if(activityId.HasValue)
                    group.Activity = _doam.SelectEconomicActivity((int) activityId);
            }

            OnClientSelected(group);

            return group;
        }

        public Group SelectGroupById(int groupId)
        {
            return SelectGroup(groupId);
        }

        public Village SelectVillageById(int id)
        {
            Village village = null;
            int? districtId = null;
            int loanOfficerId = 0;

            const string q = @"
                            SELECT Tiers.id AS tiers_id, 
                            Tiers.client_type_code, 
                            Tiers.scoring, 
                            Tiers.loan_cycle, 
                            Tiers.active,
                            Tiers.bad_client, 
                            Tiers.district_id, 
                            Tiers.city, 
                            Tiers.address, 
                            Tiers.status, 
                            Tiers.zipCode,
                            Tiers.branch_id,
                            Villages.name, 
                            Villages.establishment_date, 
                            Villages.loan_officer,
                            Villages.meeting_day
                            FROM Tiers 
                            INNER JOIN Villages ON Tiers.id = Villages.id 
                            WHERE Villages.id = @id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {

                c.AddParam("@id", id);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null && !r.Empty)
                    {
                        r.Read();

                        village = GetVillageFromReader(r);
                        districtId = r.GetNullInt("district_id");
                        loanOfficerId = r.GetInt("loan_officer");
                    }
                }
            }

            if (village != null)
            {
                village.LoanOfficer = _userManager.SelectUser(loanOfficerId, true);
                if (districtId.HasValue)
                    village.District = _locations.SelectDistrictById(districtId.Value);

                List<VillageMember> members = SelectVillageMembersByVillageId(village.Id, true);    
                
                foreach (VillageMember member in members)
                {
                    member.Tiers = SelectPersonById(member.Tiers.Id);
                    village.AddMember(member);
                    if (IsLeaderInVillage(member.Tiers.Id))
                        village.Leader = member;
                }
            }

            OnClientSelected(village);
            return village;
        }

        private Village GetVillageFromReader(OpenCbsReader r)
        {
            Village village;
            village = new Village
                          {
                              Id = r.GetInt("tiers_id"),

                              ZipCode = r.GetString("zipCode"),
                              Status =
                                  ((OClientStatus)r.GetSmallInt("status")),
                              Type = r.GetChar("client_type_code") == 'I'
                                         ? OClientTypes.Person
                                         : r.GetChar("client_type_code") == 'G'
                                               ? OClientTypes.Group
                                               : OClientTypes.Corporate,
                              Scoring = r.GetNullDouble("scoring"),
                              LoanCycle = r.GetInt("loan_cycle"),
                              Active = r.GetBool("active"),
                              BadClient = r.GetBool("bad_client")
                          };
            village.MeetingDay = (DayOfWeek?)r.GetNullInt("meeting_day");
            village.City = r.GetString("city");
            village.Address = r.GetString("address");
            village.Name = r.GetString("name");
            village.EstablishmentDate = r.GetNullDateTime("establishment_date");
            village.Branch = new Branch { Id = r.GetInt("branch_id") };
            return village;
        }

        private List<Member> SelectPersonIdsByGroupId(int groupId, bool? currentlyIn)
        {            
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                List<Member> list = SelectPersonIdsByGroupId(groupId, transaction, currentlyIn);
                transaction.Commit();
                return list;
            }            
        }

        private List<Member> SelectPersonIdsByGroupId(int groupId, SqlTransaction transac, bool? currentlyIn)
        {
            List<Member> idsList = new List<Member>();

            string q = @"SELECT person_id,
                                             is_leader,
                                             currently_in,
                                             joined_date,
                                             left_date 
                                           FROM [PersonGroupBelonging] 
                                           WHERE group_id = @id";

            if (currentlyIn.HasValue)
            {
                if (currentlyIn.Value)
                {
                    q += " AND currently_in = 1";
                }
                else
                {
                    q += " AND currently_in = 0";
                }
            }

            using (OpenCbsCommand c = new OpenCbsCommand(q, transac.Connection, transac))
            {
                c.AddParam("@id", groupId);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null)
                    {
                        while (r.Read())
                        {
                            Member member = new Member
                            {
                                Tiers = { Id = r.GetInt("person_id") },
                                CurrentlyIn = r.GetBool("currently_in"),
                                IsLeader = r.GetBool("is_leader"),
                                JoinedDate = r.GetDateTime("joined_date"),
                                LeftDate = r.GetNullDateTime("left_date"),
                            };
                            idsList.Add(member);
                        }
                    }
                }
            }
            return idsList;
        }

        public List<VillageMember> SelectVillageMembersByVillageId(int id, bool? currentlyIn)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction t = conn.BeginTransaction())
            {
                List<VillageMember> list = SelectVillageMembersByVillageId(id, t, currentlyIn);
                t.Commit();
                return list;
            }
        }

        public List<VillageMember> SelectVillageMembersByVillageId(int id, SqlTransaction t, bool? currentlyIn)
        {
            List<VillageMember> retval = new List<VillageMember>();
            string q = @"SELECT person_id
                                , joined_date
                                , left_date 
                                , is_leader
                                , currently_in                                
                                FROM VillagesPersons 
                                WHERE village_id = @id";
            if (currentlyIn.HasValue)
            {
                if (currentlyIn.Value)
                {
                    q += " AND currently_in = 1";
                }
                else
                {
                    q += " AND currently_in = 0";
                }
            }
            using (OpenCbsCommand c = new OpenCbsCommand(q, t.Connection, t))
            {
                c.AddParam("@id", id);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null)
                    {
                        while (r.Read())
                        {
                            VillageMember member = GetMemberFromReader(r);
                            retval.Add(member);
                        }
                    }
                }
            }
            return retval;
        }

        private VillageMember GetMemberFromReader(OpenCbsReader r)
        {
            return new VillageMember
                       {
                           Tiers = { Id = r.GetInt("person_id") },
                           JoinedDate = r.GetDateTime("joined_date"),
                           LeftDate = r.GetNullDateTime("left_date"),
                           IsLeader = r.GetBool("is_leader"),
                           CurrentlyIn = r.GetBool("currently_in"),
                       };
        }

        public List<Member> SelectGroupMembersByGroupId(int groupId)
        {
            List<Member> idList = SelectPersonIdsByGroupId(groupId, false);
            idList.AddRange(SelectPersonIdsByGroupId(groupId, true));

            foreach (Member entry in idList)
            {
                entry.Tiers = SelectPersonById(entry.Tiers.Id);
            }
            return idList;
        }

        public List<Member> SelectHistoryPersonsInAGroup(int groupId)
        {
            List<Member> idList = SelectPersonIdsByGroupId(groupId, false);

            foreach (Member entry in idList)
            {
                entry.Tiers = SelectPersonById(entry.Tiers.Id);
            }
            return idList;
        }

        public List<Member> SelectHistoryPersonsInAGroup(int pGroupId, int pContractId)
        {
            List<Member> members = new List<Member>();

            string q = @"SELECT 
                                  p.person_id,
                                  p.group_id, 
                                  p.is_leader, 
                                  p.currently_in,
                                  p.joined_date,
                                  p.left_date
                                FROM LoanShareAmounts l
                                INNER JOIN PersonGroupBelonging p
                                  ON l.person_id = p.person_id 
                                    AND l.group_id = p.group_id
                                WHERE contract_id = @contract_id
                                  AND l.group_id = @group_id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@group_id", pGroupId);
                c.AddParam("@contract_id", pContractId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null)
                    {
                        while (r.Read())
                        {
                            Member member = new Member
                                                {
                                                    Tiers = {Id = r.GetInt("person_id")},
                                                    CurrentlyIn = r.GetBool("currently_in"),
                                                    IsLeader = r.GetBool("is_leader"),
                                                    JoinedDate = r.GetDateTime("joined_date"),
                                                    LeftDate = r.GetNullDateTime("left_date"),
                                                };
                            members.Add(member);
                        }
                    }
                }
            }

            foreach (Member entry in members)
            {
                entry.Tiers = SelectPersonById(entry.Tiers.Id);
            }

            return members;
        }

        public void UpdateGroup(Group group)
        {
            using (SqlConnection conn = GetConnection())
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                UpdateGroup(group, transaction);
                transaction.Commit();
            }
        }

        public void UpdateGroup(Group group, SqlTransaction sqlTransac)
        {
            UpdateTiers(group, sqlTransac);

            const string q = @"UPDATE [Groups] 
                                   SET [name]=@name, 
                                     [establishment_date] = @establishmentDate, 
                                     [comments]=@comments,
                                     [meeting_day] = @meeting_day 
                                   WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@id", group.Id);
                c.AddParam("@name", group.Name);
                c.AddParam("@establishmentDate", group.EstablishmentDate);
                c.AddParam("@comments", group.Comments);
                if (group.MeetingDay.HasValue)
                {
                    c.AddParam("@meeting_day", (int) group.MeetingDay);
                }
                else
                {
                    c.AddParam("@meeting_day", null);
                }
                c.ExecuteNonQuery();
            }
            List<Member> personIdsList = SelectPersonIdsByGroupId(group.Id, sqlTransac, null);

            //loop to remove all persons
            foreach (Member member in personIdsList)
            {
                UpdatePersonFromGroup(member.Tiers.Id, group.Id, sqlTransac);
            }

            //loop to add members
            foreach (Member person in group.Members)
            {
                bool isNewPerson = true;

                foreach (Member member in personIdsList)
                {
                    if (person.Tiers.Id == member.Tiers.Id)
                    {
                        isNewPerson = false;
                        break;
                    }
                }

                if (isNewPerson)
                    AddMemberToGroup(person, group, (person.Tiers.Id == group.Leader.Tiers.Id), sqlTransac);
                else
                    UpdatePersonToGroup(person, group.Id, (person.Tiers.Id == group.Leader.Tiers.Id), sqlTransac);

                UpdatePerson((Person)person.Tiers, sqlTransac);
            }
        }

        //method needed to find one or multiple Small credit group of a person
        public ArrayList SelectGroupIdsByPersonId(int persId)
        {
            ArrayList idList = new ArrayList();

            string q = "SELECT group_id FROM PersonGroupBelonging WHERE person_id = @personId";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@personId", persId);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    while (r.Read())
                    {
                        idList.Add(r.GetInt("group_id"));
                    }
                }
            }
            return idList;
        }

        private static string BuildWhereConditionsForSearchClients(string pQuery, OClientTypes pClientType)
        {
            string whereConditions = string.Empty;

            if (pQuery != null)
            {
                if (pClientType == OClientTypes.Person)
                    whereConditions = @"AND (Persons.first_name LIKE @name 
                                        OR Persons.last_name LIKE @name 
                                        OR Persons.identification_data LIKE @passportNumber ";

                if (pClientType == OClientTypes.Group)
                    whereConditions = @"AND (Groups.name LIKE @name ";

                if (pClientType == OClientTypes.Corporate)
                    whereConditions = @"AND (Corporates.name LIKE @name 
                                        OR Corporates.siret LIKE @siret ";

                whereConditions += @"OR Districts.name LIKE @district 
                                        OR Tiers.city LIKE @city) ";
            }

            return whereConditions;
        }

        public int GetNumberOfRecordsFoundForSearchPersons(string pQuery)
        {
            string q = "SELECT COUNT(Persons.id) FROM Persons " +
                "INNER JOIN Tiers ON Persons.id = Tiers.id " +
                "INNER JOIN Districts ON Districts.id = Tiers.district_id " +
                "WHERE 1 = 1" + BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Person);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {

                c.AddParam("@name", string.Format("%{0}%", pQuery));
                c.AddParam("@passportNumber", string.Format("%{0}%", pQuery));
                c.AddParam("@district", string.Format("%{0}%", pQuery));
                c.AddParam("@city", string.Format("%{0}%", pQuery));

                return (int) c.ExecuteScalar();
            }
        }

        public List<ClientSearchResult> SearchPersonsInDatabase(int pageNumber, string pQuery)
        {
            string firstSqlText = @"SELECT TOP 20 Persons.id,Tiers.client_type_code AS type,Persons.first_name 
                + SPACE(1) + Persons.last_name AS name,Tiers.active,Tiers.loan_cycle AS loan_cycle, 
                Districts.name AS district, Tiers.city AS city,Tiers.bad_client AS bad_client, 
                Persons.identification_data AS passport_number,ISNULL((SELECT TOP 1 Groups.name from Groups 
                INNER JOIN PersonGroupBelonging on Groups.id=PersonGroupBelonging.group_id 
                WHERE PersonGroupBelonging.person_id=Persons.id),'-') AS group_name FROM Persons 
                INNER JOIN Tiers ON Persons.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id 
                WHERE 1 = 1";

            string secondSqlText = string.Format(@" AND Persons.id NOT IN (SELECT TOP {0} Persons.id FROM Persons 
                INNER JOIN Tiers ON Persons.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id 
                WHERE 1 = 1", (pageNumber - 1) * 20);

            string whereConditions = BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Person);

            string lastPartOfSqlText = "ORDER BY Persons.id ) ORDER BY Persons.id";

            string sqlText = firstSqlText + whereConditions + secondSqlText + whereConditions + lastPartOfSqlText;

            return SetClientSearchResult(sqlText, pQuery, OClientTypes.Person);
        }

        public int GetNumberOfRecordsFoundForSearchGroups(string pQuery)
        {
            string q = "SELECT COUNT(Groups.id) FROM Groups " +
                "INNER JOIN Tiers ON Groups.id = Tiers.id " +
                "INNER JOIN Districts ON Districts.id = Tiers.district_id " +
                "WHERE 1 = 1" + BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Group);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@name", string.Format("%{0}%", pQuery));
                c.AddParam("@district", string.Format("%{0}%", pQuery));
                c.AddParam("@city", string.Format("%{0}%", pQuery));
                return (int) c.ExecuteScalar();
            }
        }

        public int GetNumberOfRecordsFoundForSearchCorporates(string pQuery)
        {
            string q = "SELECT COUNT(Corporates.id) FROM Corporates " +
                "INNER JOIN Tiers ON Corporates.id = Tiers.id " +
                "INNER JOIN Districts ON Districts.id = Tiers.district_id " +
                "WHERE 1 = 1" + BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Corporate);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {

                c.AddParam("@name", string.Format("%{0}%", pQuery));
                c.AddParam("@district", string.Format("%{0}%", pQuery));
                c.AddParam("@city", string.Format("%{0}%", pQuery));
                c.AddParam("@siret", string.Format("%{0}%", pQuery));
                return (int)c.ExecuteScalar();
            }
        }

        public bool IsThisIdentificationDataAlreadyUsed(string pIdentificationData, int pId)
        {
            string q = pId == 0 ? "SELECT COUNT(*) FROM Persons WHERE identification_data = @data" : "SELECT COUNT(*) FROM Persons WHERE identification_data = @data AND id <> @id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@data", pIdentificationData);
                if (pId != 0)
                    c.AddParam("@id", pId);

                int count = (int) c.ExecuteScalar();
                return (count > 0);
            }
        }

        public List<Person> IsThereSimilardentificationDataAlreadyUsed(string pIdentificationData, int pID)
        {
            string beginning = pIdentificationData;
            string end = pIdentificationData;
            string param = "identification_data LIKE '_" + beginning.Substring(1) + "'";
            for(int i = 1; i< pIdentificationData.Length; i++)
            {
                beginning = end = pIdentificationData;
                param += " OR identification_data LIKE '" + beginning.Substring(0, i) + "_" + end.Substring(i + 1, end.Length - i - 1) + "'";
            }


            string q = "SELECT id, first_name, last_name, identification_data FROM Persons WHERE id!=@id AND (" + param + ")";
            List<Person> _persons = new List<Person>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pID);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            Person p = new Person
                                           {
                                               Id = r.GetInt("id"),
                                               FirstName = r.GetString("first_name"),
                                               LastName = r.GetString("last_name"),
                                               IdentificationData = r.GetString("identification_data")
                                           };
                            _persons.Add(p);
                        }
                    }
                }
            }
            return _persons;
        }
        
        public List<ClientSearchResult> SetClientSearchResult(string q, string pQuery, OClientTypes pClientType)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {

                c.AddParam("@name", string.Format("%{0}%", pQuery));
                c.AddParam("@district", string.Format("%{0}%", pQuery));
                c.AddParam("@city", string.Format("%{0}%", pQuery));

                if (pClientType == OClientTypes.Person)
                {
                    c.AddParam("@passportNumber", string.Format("%{0}%", pQuery));
                }

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            ClientSearchResult result = new ClientSearchResult();
                            result.Id = r.GetInt("id");
                            result.Type = r.GetChar("type") == 'I'
                                              ? OClientTypes.Person
                                              : OClientTypes.Group;
                            result.Name = r.GetString("name");
                            result.Active = r.GetBool("active");
                            result.LoanCycle = pClientType == OClientTypes.Group
                                                   ? 0
                                                   : r.GetInt("loan_cycle");
                            result.District = r.GetString("district");
                            result.City = r.GetString("city");
                            result.BadClient = r.GetBool("bad_client");
                            result.PassportNumber = r.GetString("passport_number");
                            result.MemberOf = r.GetString("group_name");
                            list.Add(result);
                        }
                    }
                }
            }
            return list;
        }

        public List<ClientSearchResult> SetCorporatesSearchResult(string q, string pQuery, OClientTypes pClientType)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@name", string.Format("%{0}%", pQuery));
                c.AddParam("@district", string.Format("%{0}%", pQuery));
                c.AddParam("@city", string.Format("%{0}%", pQuery));
                c.AddParam("@siret", string.Format("%{0}%", pQuery));

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            ClientSearchResult result = new ClientSearchResult();
                            result.Id = r.GetInt("id");
                            result.Name = r.GetString("name");
                            result.Active = r.GetBool("active");
                            result.District = r.GetString("district");
                            result.City = r.GetString("city");
                            result.Siret = r.GetString("siret");
                            result.Type = OClientTypes.Corporate;
                            list.Add(result);
                        }
                    }
                }
            }
            return list;
        }

        public List<ClientSearchResult> SearchAllPersonsInDatabase(string pQuery)
        {
            string firstSqlText = @"SELECT Persons.id,Tiers.client_type_code AS type,Persons.first_name 
                + SPACE(1) +Persons.last_name AS name,Tiers.active,Tiers.loan_cycle AS loanCycle, 
                Districts.name AS district, Tiers.city AS city, Tiers.bad_client AS bad_client, Persons.identification_data AS passport_number, 
                ISNULL((SELECT TOP 1 Groups.name from Groups 
                INNER JOIN PersonGroupBelonging on Groups.id=PersonGroupBelonging.group_id WHERE PersonGroupBelonging.person_id=Persons.id),'-') 
                AS group_name FROM Persons 
                INNER JOIN Tiers ON Persons.id = Tiers.id  
                INNER JOIN Districts ON Districts.id = Tiers.district_id 
                WHERE 1 = 1";

            string whereConditions = BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Person);
            string sqlText = firstSqlText + whereConditions;

            return SetClientSearchResult(sqlText, pQuery, OClientTypes.Group);
        }

        public List<ClientSearchResult> SearchAllGroupsInDatabase(string pQuery)
        {
            string firstSqlText = "SELECT Groups.id,Tiers.client_type_code AS type,Groups.name,Tiers.active,Tiers.loan_cycle AS loanCycle, " +
                "Districts.name AS district, Tiers.city AS city,Tiers.bad_client AS bad_client, '-' AS passport_number, '-' AS group_name FROM Groups " +
                "INNER JOIN Tiers ON Groups.id = Tiers.id " +
                "INNER JOIN Districts ON Districts.id = Tiers.district_id " +
                "WHERE 1 = 1 ";

            string whereConditions = BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Group);
            string sqlText = firstSqlText + whereConditions;

            return SetClientSearchResult(sqlText, pQuery, OClientTypes.Group);
        }

        public List<ClientSearchResult> SearchCorporate(int pageNumber, string pQuery)
        {
            string firstSqlText = @"SELECT TOP 20 Corporates.id,Corporates.name,Corporates.siret,Tiers.active,
                Districts.name AS district, Tiers.city AS city FROM Corporates INNER JOIN Tiers ON Corporates.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id 
                WHERE 1 = 1 ";
            string secondSqlText = " AND Corporates.id NOT IN(SELECT TOP " + (pageNumber - 1) * 20 + @" Corporates.id FROM Corporates
                INNER JOIN Tiers ON Corporates.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id
                WHERE 1 = 1 ";
            string whereConditions = BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Corporate);
            string lastPartOfSqlText = "ORDER BY Corporates.id) " + "ORDER BY Corporates.id";

            string sqlText = firstSqlText + whereConditions + secondSqlText + whereConditions + lastPartOfSqlText;
            return SetCorporatesSearchResult(sqlText, pQuery, OClientTypes.Corporate);
        }

        public List<ClientSearchResult> SearchGroups(int pageNumber, string pQuery)
        {
            string firstSqlText = "SELECT TOP 20 Groups.id,Tiers.client_type_code AS type,Groups.name,Tiers.active,Tiers.loan_cycle AS loanCycle, " +
                "Districts.name AS district, Tiers.city AS city, Tiers.bad_client AS bad_client, '-' AS passport_number, '-' AS group_name FROM Groups " +
                "INNER JOIN Tiers ON Groups.id = Tiers.id " +
                "INNER JOIN Districts ON Districts.id = Tiers.district_id " +
                "WHERE 1 = 1";

            string secondSqlText = "AND Groups.id NOT IN (SELECT TOP " + (pageNumber - 1) * 20 + " Groups.id FROM Groups " +
                "INNER JOIN Tiers ON Groups.id = Tiers.id " +
                "INNER JOIN Districts ON Districts.id = Tiers.district_id " +
                "WHERE 1 = 1";

            string whereConditions = BuildWhereConditionsForSearchClients(pQuery, OClientTypes.Group);
            string lastPartOfSqlText = "ORDER BY Groups.id) " + "ORDER BY Groups.id";

            string sqlText = firstSqlText + whereConditions + secondSqlText + whereConditions + lastPartOfSqlText;
            return SetClientSearchResult(sqlText, pQuery, OClientTypes.Group);
        }

        public List<Person> FindAllPersons()
        {
            List<Person> result = new List<Person>();
           
            Person person = null;
            int? districtId = null;
            int? secondaryDistrictId = null;
            int? activityId = null;
            int? personalBankId = null;
            int? businessBankId = null;

            string q = @"SELECT 
                                 Tiers.id AS tiers_id, 
                                 Tiers.client_type_code, 
                                 Tiers.scoring, 
                                 Tiers.loan_cycle,
                                 Tiers.active, 
                                 Tiers.bad_client, 
                                 Tiers.other_org_name, 
                                 Tiers.other_org_amount, 
                                 Tiers.other_org_debts, 
                                 Tiers.district_id, 
                                 Tiers.city, 
                                 Tiers.address, 
                                 Tiers.secondary_district_id, 
                                 Tiers.secondary_city, 
                                 Tiers.secondary_address, 
                                 Tiers.cash_input_voucher_number, 
                                 Tiers.cash_output_voucher_number,
                                 Tiers.status,
                                 Tiers.home_phone, 
                                 Tiers.personal_phone,
                                 Tiers.secondary_home_phone,
                                 Tiers.secondary_personal_phone,
                                 Tiers.e_mail,
                                 Tiers.secondary_e_mail,
                                 Tiers.home_type,
                                 Tiers.zipCode, 
                                 Tiers.secondary_zipCode,
                                 Tiers.secondary_homeType,
                                 Tiers.other_org_comment,
                                 Tiers.sponsor1,Tiers.sponsor2,
                                 Tiers.sponsor1_comment,Tiers.sponsor2_comment,
                                 Tiers.follow_up_comment,
                                 Tiers.branch_id,
                                 Persons.first_name, 
                                 Persons.sex, 
                                 Persons.identification_data, 
                                 Persons.last_name, 
                                 Persons.birth_date, 
                                 Persons.household_head, 
                                 Persons.nb_of_dependents, 
                                 Persons.nb_of_children, 
                                 Persons.children_basic_education, 
                                 Persons.livestock_number, 
                                 Persons.livestock_type, 
                                 Persons.landplot_size, 
                                 Persons.home_time_living_in, 
                                 Persons.home_size, 
                                 Persons.capital_other_equipments, 
                                 Persons.activity_id, 
                                 Persons.experience, 
                                 Persons.nb_of_people, 
                                 Persons.mother_name, 
                                 Persons.image_path, 
                                 Persons.father_name, 
                                 Persons.birth_place, 
                                 Persons.nationality,
                                 Persons.unemployment_months,
                                 Persons.personalBank_id,
                                 Persons.businessBank_id, 
                                 Persons.study_level, 
                                 Persons.SS, 
                                 Persons.CAF, 
                                 Persons.housing_situation, 
                                 Persons.handicapped,
                                 Persons.professional_situation, 
                                 Persons.professional_experience,
                                 Persons.first_contact,
                                 Persons.first_appointment, 
                                 Persons.family_situation,
                                 Persons.povertylevel_childreneducation,
                                 Persons.povertylevel_economiceducation,
                                 Persons.povertylevel_socialparticipation,
                                 Persons.povertylevel_healthsituation,
                                 Persons.loan_officer_id      
                            FROM Tiers 
                            INNER JOIN Persons ON Tiers.id = Persons.id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {

                        while (r.Read())
                        {
                            person = GetPersonFromReader(r);
                            personalBankId = r.GetNullInt("personalBank_id");
                            businessBankId = r.GetNullInt("businessBank_id");
                            secondaryDistrictId = r.GetNullInt("secondary_district_id");
                            activityId = r.GetNullInt("activity_id");
                            districtId = r.GetNullInt("district_id");
                            if (person != null)
                            {
                                UserManager userManager = new UserManager(User.CurrentUser);
                                if (person.FavouriteLoanOfficerId.HasValue)
                                    person.FavouriteLoanOfficer = userManager.SelectUser((int)person.FavouriteLoanOfficerId,
                                                                                         true);

                                if (activityId.HasValue)
                                    person.Activity = _doam.SelectEconomicActivity(activityId.Value);

                                if (districtId.HasValue)
                                    person.District = _locations.SelectDistrictById(districtId.Value);

                                if (secondaryDistrictId.HasValue)
                                    person.SecondaryDistrict = _locations.SelectDistrictById(secondaryDistrictId.Value);

                                if (person != null)
                                {
                                    if (_projectManager != null)
                                        person.AddProjects(_projectManager.SelectProjectsByClientId(person.Id));
                                    if (_savingManager != null)
                                        person.AddSavings(_savingManager.SelectSavings(person.Id));
                                }

                                OnClientSelected(person);
                            }
                           result.Add(person);
                        }
                     }
                }
            }
           return result;
        }

        public List<ClientSearchResult> SearchPersonByCriteres(int pageNumber, string pQuery)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();

            string SELECT_FROM_PROJET_ = @"		SELECT  TOP 100 percent pers.id, Tiers.client_type_code AS type,pers.first_name 
                + SPACE(1) + pers.last_name AS name,Tiers.active,Tiers.loan_cycle AS loan_cycle, 
                dis.name AS district, Tiers.city AS city,Tiers.bad_client AS bad_client, 
                pers.identification_data AS passport_number,ISNULL((SELECT TOP 1 Groups.name from Groups 
                INNER JOIN PersonGroupBelonging on Groups.id=PersonGroupBelonging.group_id WHERE PersonGroupBelonging.person_id=pers.id),'-') AS group_name FROM Persons pers 
                INNER JOIN Tiers ON pers.id = Tiers.id
                INNER JOIN Districts dis ON dis.id = Tiers.district_id ORDER BY pers.id ) maTable";

            string CloseWhere = @" WHERE ( passport_number LIKE @passeportNumber OR name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";
            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);
            string pSqlText = q.ConstructSQLEntityByCriteresProxy(20, (pageNumber - 1) * 20);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {

                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }

                OClientTypes pClientType = OClientTypes.Person;

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            ClientSearchResult result = new ClientSearchResult();
                            result.Id = r.GetInt("id");
                            result.Type = r.GetChar("type") == 'I'
                                              ? OClientTypes.Person
                                              : OClientTypes.Group;
                            result.Name = r.GetString("name");
                            result.Active = r.GetBool("active");
                            result.LoanCycle = pClientType == OClientTypes.Group
                                                   ? 0
                                                   : r.GetInt("loan_cycle");
                            result.District = r.GetString("district");
                            result.City = r.GetString("city");
                            result.BadClient = r.GetBool("bad_client");
                            result.PassportNumber = r.GetString("passport_number");
                            result.MemberOf = r.GetString("group_name");
                            list.Add(result);
                        }
                    }
                }
            }
            return list;
        }

        public List<ClientSearchResult> SearchInactivePersonsByCriteres(int pageNumber, string query)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();
            const string sql = @"SELECT TOP 100 PERCENT p.id, t.client_type_code AS type, 
                p.first_name + SPACE(1) + p.last_name AS name, t.active, t.loan_cycle,
                d.name AS district, t.city, t.bad_client, p.identification_data AS passport_number
                FROM Persons AS p
                LEFT JOIN Tiers AS t ON p.id = t.id
                LEFT JOIN Districts AS d ON t.district_id = d.id WHERE active = 0 
                and Tiers.branch_id in (
                select branch_id
                from dbo.UsersBranches
                where user_id = @user_id
            )
            ) maTable";
            const string where = @" WHERE active = 0 AND ( passport_number LIKE @passportNumber OR name LIKE @name OR
                district LIKE @district OR city LIKE @city )) maTable";
            QueryEntity q = new QueryEntity(query, sql, where);
            string sqlText = q.ConstructSQLEntityByCriteresProxy(20, (pageNumber - 1) * 20);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(sqlText, conn))
            {
                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }
                c.AddParam("@user_id", User.CurrentUser.Id);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (null == r || r.Empty) return list;
                    while (r.Read())
                    {
                        ClientSearchResult result = new ClientSearchResult();
                        result.Id = r.GetInt("id");
                        result.Type = OClientTypes.Person;
                        result.Name = r.GetString("name");
                        result.Active = r.GetBool("active");
                        result.Active = r.GetBool("active");
                        result.LoanCycle = r.GetInt("loan_cycle");
                        result.District = r.GetString("district");
                        result.City = r.GetString("city");
                        result.BadClient = r.GetBool("bad_client");
                        result.PassportNumber = r.GetString("passport_number");
                        result.MemberOf = "--";
                        list.Add(result);
                    }
                }
            }
            return list;
        }

        public List<ClientSearchResult> SearchCorporateByCriteres(int onlyActive, int pageNumber, string pQuery)
        {
            string sign;
            if (onlyActive == 0 || onlyActive == 1) { sign = "="; } // active or not active clients
            else if (onlyActive == 2) { sign = "<"; } // everyone
            else { sign = ">"; } // no one
            
            List<ClientSearchResult> list = new List<ClientSearchResult>();
            string SELECT_FROM_PROJET_ = String.Format(@"SELECT TOP 100 percent 
                                           Corporates.id, 
                                           Corporates.name,
                                           Corporates.siret,
                                           Tiers.active,
                                           Districts.name AS district,
                                           Tiers.loan_cycle AS loan_cycle,
                                           Tiers.city AS city 
                                           FROM Corporates 
                                           INNER JOIN Tiers ON Corporates.id = Tiers.id 
                                           INNER JOIN Districts ON Districts.id = Tiers.district_id 
                                           WHERE active {0} {1}
                                           ORDER BY Corporates.id 
                                           ) maTable", sign, onlyActive);

            string CloseWhere = @" WHERE ( siret LIKE @siret OR name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";

            var q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);
            string pSqlText = q.ConstructSQLEntityByCriteresProxy(20, (pageNumber - 1) * 20);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {
                foreach (var item in q.DynamiqParameters())
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            var result = new ClientSearchResult
                                             {
                                                 Id = r.GetInt("id"),
                                                 Name = r.GetString("name"),
                                                 Active = r.GetBool("active"),
                                                 District = r.GetString("district"),
                                                 City = r.GetString("city"),
                                                 Siret = r.GetString("siret"),
                                                 Type = OClientTypes.Corporate,
                                                 LoanCycle = r.GetInt("loan_cycle")
                                             };
                            list.Add(result);
                        }
                    }
                }
            }
            return list;
        }

        public List<ClientSearchResult> SearchGroupByCriteres(int pageNumber, string pQuery)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();
            string SELECT_FROM_PROJET_ = @"		SELECT TOP 100 percent Groups.id,Tiers.client_type_code AS type,Groups.name,Tiers.active,Tiers.loan_cycle AS loan_cycle, 
                Districts.name AS district, Tiers.city AS city, Tiers.bad_client AS bad_client, '-' AS passport_number, '-' AS group_name FROM Groups 
                INNER JOIN Tiers ON Groups.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id ORDER BY Groups.id  ) maTable";

            string CloseWhere = @" WHERE (name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";

            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);
            string pSqlText = q.ConstructSQLEntityByCriteresProxy(20, (pageNumber - 1) * 20);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {

                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            ClientSearchResult result = new ClientSearchResult();
                            result.Id = r.GetInt("id");
                            result.Type = r.GetChar("type") == 'I'
                                              ? OClientTypes.Person
                                              : OClientTypes.Group;
                            result.Name = r.GetString("name");
                            result.Active = r.GetBool("active");
                            result.LoanCycle = r.GetInt("loan_cycle");
                            result.District = r.GetString("district");
                            result.City = r.GetString("city");
                            result.BadClient = r.GetBool("bad_client");
                            result.PassportNumber = r.GetString("passport_number");
                            result.MemberOf = r.GetString("group_name");
                            list.Add(result);
                        }
                    }
                }
            }
            return list;
        }

        public List<ClientSearchResult> SearchVillagesByCriteres(int pageNumber, string pQuery, int isActive)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();
            string SELECT_FROM_PROJET_ = @"		SELECT TOP 100 percent Villages.id,Tiers.client_type_code AS type,Villages.name,Tiers.active,Tiers.loan_cycle AS loan_cycle, 
                Districts.name AS district, Tiers.city AS city, Tiers.bad_client AS bad_client, '-' AS passport_number, '-' AS village_name FROM Villages 
                INNER JOIN Tiers ON Villages.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id ORDER BY Villages.id  ) maTable";

            string CloseWhere = @" WHERE (name LIKE @name OR district LIKE @district OR city LIKE @city ) and active = @isActive) maTable";

            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);

            string pSqlText = q.ConstructSQLEntityByCriteresProxy(20, (pageNumber - 1) * 20);

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {

                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (!r.Empty)
                    {
                        while (r.Read())
                        {
                            ClientSearchResult result = new ClientSearchResult();
                            result.Id = r.GetInt("id");
                            result.Type = OClientTypes.Village;
                            result.Name = r.GetString("name");
                            result.Active = r.GetBool("active");
                            result.LoanCycle = r.GetInt("loan_cycle");
                            result.District = r.GetString("district");
                            result.City = r.GetString("city");
                            result.BadClient = r.GetBool("bad_client");
                            result.PassportNumber = r.GetString("passport_number");
                            result.MemberOf = r.GetString("village_name");
                            list.Add(result);
                        }
                    }
                }
            }
            return list;
        }

        public int GetNumberCorporate(string pQuery)
        {
            string SELECT_FROM_PROJET_ = @"SELECT TOP 100 percent 
                                          Corporates.id,
                                          Corporates.name,
                                          Corporates.siret,
                                          Tiers.active,
                                          Districts.name AS district,
                                          Tiers.city AS city 
                                          FROM Corporates 
                                          INNER JOIN Tiers ON Corporates.id = Tiers.id 
                                          INNER JOIN Districts ON Districts.id = Tiers.district_id 
                                          ORDER BY Corporates.id   ) maTable";

            string CloseWhere = @" WHERE ( siret LIKE @siret  OR name LIKE @name OR district LIKE @district OR city LIKE  @city )) maTable";

            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);

            string pSqlText = q.ConstructSQLEntityNumberProxy();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {

                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }
                return (int) c.ExecuteScalar();
            }
        }

        public int GetNumberPerson(string pQuery, int isActive)
        {
            string SELECT_FROM_PROJET_ = @"SELECT  TOP 100 percent pers.id,Tiers.client_type_code AS type,pers.first_name 
                + SPACE(1) + pers.last_name AS name,Tiers.active,Tiers.loan_cycle AS loan_cycle, 
                dis.name AS district, Tiers.city AS city,Tiers.bad_client AS bad_client, 
                pers.identification_data AS passport_number,ISNULL((SELECT TOP 1 Groups.name from Groups 
                INNER JOIN PersonGroupBelonging on Groups.id=PersonGroupBelonging.group_id 
                WHERE PersonGroupBelonging.person_id=pers.id),'-') AS group_name FROM Persons pers 
                INNER JOIN Tiers ON pers.id = Tiers.id
                INNER JOIN Districts dis ON dis.id = Tiers.district_id ORDER BY pers.id ) maTable";

            string CloseWhere = @" WHERE ( passport_number LIKE @passeportNumber OR name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";
            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);
            string pSqlText = q.ConstructSQLEntityNumberProxy();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {

                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }
                return (int) c.ExecuteScalar();
            }
        }

        public int GetNumberGroup(string pQuery)
        {
            string SELECT_FROM_PROJET_ = @"SELECT TOP 100 percent Groups.id,Tiers.client_type_code AS type,Groups.name,Tiers.active,Tiers.loan_cycle AS loanCycle, 
                Districts.name AS district, Tiers.city AS city, Tiers.bad_client AS bad_client, '-' AS passport_number, '-' AS group_name FROM Groups 
                INNER JOIN Tiers ON Groups.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id ORDER BY Groups.id  ) maTable";

            string CloseWhere = @" WHERE ( name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";
            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);

            string pSqlText = q.ConstructSQLEntityNumberProxy();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {

                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }
                return (int) c.ExecuteScalar();
            }
        }

        public int GetNumberVillages(string pQuery)
        {
            string SELECT_FROM_PROJET_ = @"SELECT TOP 100 percent Villages.id,Tiers.client_type_code AS type,Villages.name,Tiers.active,Tiers.loan_cycle AS loanCycle, 
                Districts.name AS district, Tiers.city AS city, Tiers.bad_client AS bad_client, '-' AS passport_number, '-' AS village_name FROM Villages 
                INNER JOIN Tiers ON Villages.id = Tiers.id 
                INNER JOIN Districts ON Districts.id = Tiers.district_id ORDER BY Villages.id  ) maTable";

            string CloseWhere = @" WHERE ( name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";
            QueryEntity q = new QueryEntity(pQuery, SELECT_FROM_PROJET_, CloseWhere);

            string pSqlText = q.ConstructSQLEntityNumberProxy();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(pSqlText, conn))
            {
                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }
                return (int) c.ExecuteScalar();
            }
        }

        public int GetFoundRecordsNumber(string query, int isActive, int includePersons, int includeGroups, int includeVillages)
        {
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = GenerateSearchQuery(true, query, isActive, 0, includePersons, includeGroups, includeVillages, conn))
                return (int) c.ExecuteScalar();
        }

        public List<ClientSearchResult> SearchAllByCriteres(string pQuery, int activeOnly, int currentPage, int includePersons, int includeGroups, int includeVillages)
        {
            List<ClientSearchResult> list = new List<ClientSearchResult>();


            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = GenerateSearchQuery(false, pQuery, activeOnly, currentPage, includePersons, includeGroups, includeVillages, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (!r.Empty)
                {
                    while (r.Read())
                    {
                        ClientSearchResult result = new ClientSearchResult();
                        result.Id = r.GetInt("id");
                        switch (r.GetChar("type"))
                        {
                            case 'I': result.Type = OClientTypes.Person; break;
                            case 'G': result.Type = OClientTypes.Group; break;
                            case 'V': result.Type = OClientTypes.Village; break;
                            case 'C': result.Type = OClientTypes.Corporate; break;
                            default: result.Type = OClientTypes.Person; break;
                        }
                        result.Name = r.GetString("name");
                        result.Active = r.GetBool("active");
                        result.LoanCycle = r.GetInt("loan_cycle");
                        result.District = r.GetString("district");
                        result.City = r.GetString("city");
                        result.BadClient = r.GetBool("bad_client");
                        result.PassportNumber = r.GetString("passport_number");
                        result.MemberOf = r.GetString("group_name");
                        list.Add(result);
                    }
                }
            }
            return list;
        }

        private OpenCbsCommand GenerateSearchQuery(bool count, String query, int activeOnly, int currentPage,
                                       int includePersons, int includeGroups, int includeVillages, SqlConnection conn)
        {
            string sign;
            if (activeOnly == 0 || activeOnly == 1) { sign = "="; } // active or not active clients
            else if (activeOnly == 2) { sign = "<"; } // everyone
            else { sign = ">"; } // no one

            int startRow = 0 + (currentPage - 1) * 20 + 1;
            int endRow = currentPage * 20;
            string sqlText = count ? 
                                @"	SELECT count(id) FROM " : 
                                @" SELECT * 
                                FROM (
                                        SELECT Row_Number() over (order by id) _rowNum, * 
                                        FROM ";
            
            
            sqlText += string.Format(@"
		         ( 
			         SELECT pers.id as id
                            ,Tiers.client_type_code AS type
                            ,pers.first_name + SPACE(1) + pers.last_name AS name
                            ,Tiers.active
                            ,Tiers.loan_cycle AS loan_cycle
                            ,dis.name AS district
                            ,Tiers.city AS city
                            ,Tiers.bad_client AS bad_client
                            ,pers.identification_data AS passport_number
                            ,COALESCE(
                                            (SELECT TOP 1 Groups.name 
                                             FROM Groups 
                                             INNER JOIN PersonGroupBelonging on Groups.id = PersonGroupBelonging.group_id 
                                             WHERE GETDATE() BETWEEN joined_date AND left_date
                                                   AND PersonGroupBelonging.person_id = pers.id),

                                            (SELECT TOP 1 Groups.name 
                                             FROM Groups 
                                             INNER JOIN PersonGroupBelonging on Groups.id = PersonGroupBelonging.group_id 
                                             WHERE joined_date < GETDATE() AND left_date IS NULL
                                                   AND PersonGroupBelonging.person_id = pers.id),

                                            (SELECT TOP 1 Villages.name 
                                             FROM Villages 
                                             INNER JOIN VillagesPersons on Villages.id = VillagesPersons.village_id 
                                             WHERE GETDATE() BETWEEN joined_date AND left_date
                                                  AND VillagesPersons.person_id = pers.id),

                                            (SELECT TOP 1 Villages.name 
                                             FROM Villages 
                                             INNER JOIN VillagesPersons on Villages.id = VillagesPersons.village_id 
                                              WHERE joined_date < GETDATE() AND left_date IS NULL
                                                        AND VillagesPersons.person_id = pers.id),

                                                '-'
                                        ) AS group_name 
			         FROM Persons pers
                          INNER JOIN Tiers ON pers.id = Tiers.id
                          INNER JOIN Districts dis ON dis.id = Tiers.district_id 
			         WHERE ( pers.identification_data LIKE @passport OR pers.first_name LIKE @name
                          OR pers.last_name LIKE @name OR dis.name LIKE @district OR Tiers.city LIKE @city )  
                          and active {0} @active and 1 = @includePersons
                        and Tiers.branch_id in (
                            select branch_id
                            from dbo.UsersBranches
                            where user_id = @user_id
                        )

			         UNION ALL

			         SELECT  Villages.id
                            ,Tiers.client_type_code AS type
                            ,Villages.name as name
                            ,Tiers.active
                            ,Tiers.loan_cycle AS loan_cycle
                            ,Districts.name AS district
                            ,Tiers.city AS city
                            ,Tiers.bad_client AS bad_client
                            ,'-' AS passport_number
                            ,'-' AS village_name 
			         FROM Villages 
				          INNER JOIN Tiers ON Villages.id = Tiers.id
				          INNER JOIN Districts ON Districts.id = Tiers.district_id
			         WHERE (Villages.name LIKE @name OR Districts.name  LIKE  @district OR Tiers.city  LIKE @city ) 
                          and active {0} @active and 1 = @includeVillages
                        and Tiers.branch_id in (
                            select branch_id
                            from dbo.UsersBranches
                            where user_id = @user_id
                        )

			         UNION ALL

			         SELECT Groups.id
                           ,Tiers.client_type_code AS type
                           ,Groups.name as name
                           ,Tiers.active
                           ,Tiers.loan_cycle AS loan_cycle
                           ,Districts.name AS district
                           ,Tiers.city AS city
                           ,Tiers.bad_client AS bad_client
                           ,'-' AS passport_number
                           ,'-' AS group_name 
			         FROM Groups
				          INNER JOIN Tiers ON Groups.id = Tiers.id 
				          INNER JOIN Districts ON Districts.id = Tiers.district_id 
			         WHERE (Groups.name LIKE @name OR Districts.name LIKE  @district OR Tiers.city LIKE @city ) 
                          AND active {0} @active AND 1 = @includeGroups
                          AND Tiers.branch_id in (
                            select branch_id
                            from dbo.UsersBranches
                            where user_id = @user_id
                            )
                          ) maTable ", sign);

            if (!count) sqlText += ") matable where matable._rowNum between @startRow and @endRow order by name";

            var select = new OpenCbsCommand(sqlText, conn);
            
            select.AddParam("@district", "%" + query + "%");
            select.AddParam("@city", "%" + query + "%");
            select.AddParam("@name", "%" + query + "%");
            select.AddParam("@passport", "%" + query + "%");
            select.AddParam("@siret", "%" + query + "%");
            
            select.AddParam("@active", activeOnly);
            select.AddParam("@startRow", startRow);
            select.AddParam("@endRow", endRow);

            select.AddParam("@includeVillages", includeVillages);
            select.AddParam("@includeGroups", includeGroups);
            select.AddParam("@includePersons", includePersons);

            select.AddParam("@user_id", User.CurrentUser.Id);

            return select;
        }

        public int GetNumberInactivePersons(string query)
        {
            const string sql = @"SELECT TOP 100 PERCENT Persons.id,
                                Persons.first_name + ' ' + Persons.last_name AS name,
                                Districts.name AS district,
                                Tiers.city,
                                Tiers.active
                                FROM Persons
                                LEFT JOIN Tiers ON Persons.id = Tiers.id
                                LEFT JOIN Districts ON Tiers.district_id = Districts.id WHERE active = 0 
            where Tiers.branch_id in (
                select branch_id
                from dbo.UsersBranches
                where user_id = @user_id
            )
            ) maTable";
            const string where =
                @" WHERE active = 0 AND ( name LIKE @name OR district LIKE @district OR city LIKE @city )) maTable";
            QueryEntity q = new QueryEntity(query, sql, where);
            string sqlText = q.ConstructSQLEntityNumberProxy();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(sqlText, conn))
            {
                foreach (var item in q.DynamiqParameters())
                {
                    c.AddParam(item.Key, string.Format("%{0}%", item.Value));
                }
                c.AddParam("@user_id", User.CurrentUser.Id);
                return (int) c.ExecuteScalar();
            }
        }

        public bool IsGroupNameAlreadyUsedInDistrict(string pName, District pDistrict)
        {
            string q = @"SELECT COUNT(*) FROM Tiers INNER JOIN Groups ON Tiers.id = Groups.id WHERE { fn UCASE(Groups.name) } = @name AND Tiers.district_id=@disctrictId";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@name", pName.ToUpper());
                c.AddParam("@disctrictId", pDistrict.Id);
                int count = (int) c.ExecuteScalar();
                return (count > 0);
            }
        }

        public IClient SelectClientByContractId(int pId)
        {
            string q = @"SELECT DISTINCT Tiers.id 
                               FROM Contracts 
                               INNER JOIN Projects ON Projects.id = Contracts.project_id
                               INNER JOIN Tiers ON Tiers.id = Projects.tiers_id
                               WHERE Contracts.id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pId);

                int clientId = Convert.ToInt32(c.ExecuteScalar());

                IClient client = SelectGroup(clientId);
                if (client == null)
                    client = SelectPersonById(clientId);
                if (client == null)
                    client = SelectBodyCorporateById(clientId);

                return client;
            }
        }

        public IClient SelectClientById(int id)
        {
            IClient client = SelectGroup(id);
            if (client == null)
                client = SelectPersonById(id);
            if (client== null)
            client = SelectBodyCorporateById(id);

            return client;
        }

        public IClient SelectClientByProjectId(int pId)
        {
            string q = @"SELECT DISTINCT Tiers.id 
                               FROM Tiers 
                               INNER JOIN Projects ON Tiers.id = Projects.tiers_id
                               WHERE Projects.id = @id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pId);

                int clientId = Convert.ToInt32(c.ExecuteScalar());

                IClient client = SelectGroup(clientId);
                if (client == null)
                    client = SelectPersonById(clientId);
                if (client == null)
                    client = SelectBodyCorporateById(clientId);

                return client;
            }
        }

        public IClient SelectClientBySavingsId(int pId)
        {
            string q = @"SELECT SavingContracts.tiers_id
                               FROM SavingContracts
                               WHERE SavingContracts.id =@id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", pId);

                int clientId = Convert.ToInt32(c.ExecuteScalar());

                IClient client = SelectGroup(clientId);
                if (client == null)
                    client = SelectPersonById(clientId);
                if (client == null)
                    client = SelectBodyCorporateById(clientId);

                return client;
            }
        }

        public int AddBodyCorporate(Corporate body, FundingLine pFundingLine, SqlTransaction sqlTransac)
        {
            body.Id = AddTiers(body, sqlTransac);

            const string q = @"INSERT INTO [Corporates] ([id],[name],[deleted],[sigle],[small_name],[volunteer_count],
            [agrement_date],[agrement_solidarity],[insertionType],[employee_count],[siret],[activity_id],[legalForm],[fiscal_status],[registre],[date_create]) 
            VALUES(@id,@name,@deleted,@sigle,@small_name,@volunteer_count,@agrement_date,@agrement_solidarity,
            @insertionType,@employee_count,@siret,@activity_id,@legalForm,@fiscalStatus,@registre, @date_create)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@id", body.Id);
                c.AddParam("@name", body.Name);
                c.AddParam("@deleted", body.IsDeleted);
                c.AddParam("@sigle", body.Sigle);
                c.AddParam("@small_name", body.SmallName);
                c.AddParam("@volunteer_count", body.VolunteerCount);

                c.AddParam("@agrement_solidarity", body.AgrementSolidarity);
                c.AddParam("@employee_count", body.EmployeeCount);
                c.AddParam("@siret", body.Siret);
                c.AddParam("@fiscalStatus", body.FiscalStatus);
                c.AddParam("@registre", body.Registre);

                c.AddParam("@insertionType", body.InsertionType);
                c.AddParam("@legalForm", body.LegalForm);

                if (body.Activity != null)
                    c.AddParam("@activity_id", body.Activity.Id);
                else
                    c.AddParam("@activity_id", null);

                c.AddParam("@agrement_date"
                    , body.AgrementDate != DateTime.MinValue ? body.AgrementDate : null
                    );

                c.AddParam("@date_create"
                    , body.CreationDate != DateTime.MinValue ? (object)body.RegistrationDate : null
                    );


                c.ExecuteScalar();
            }
            return body.Id;
        }

        public void AddBodyCorporatePerson(int pCorporateId, Contact contact, SqlTransaction sqlTransac)
        {
            string q = "INSERT INTO [CorporatePersonBelonging] (corporate_id, person_id, position) VALUES (@corporate_id, @person_id, @position)";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@corporate_id", pCorporateId);
                c.AddParam("@person_id", contact.Tiers.Id);
                c.AddParam("@position", contact.Position);
                c.ExecuteScalar();
            }
        }

        public void DeleteBodyPersonByCorporate(int pCorporateId, SqlTransaction sqlTransac)
        {
            string q = "DELETE FROM [CorporatePersonBelonging] WHERE corporate_id=@corporate_id";
            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@corporate_id", pCorporateId);
                c.ExecuteScalar();
            }
        }

        public void UpdateBodyCorporate(Corporate body, SqlTransaction sqlTransac)
        {
            UpdateTiers(body, sqlTransac);

            const string q = @"UPDATE [Corporates] SET [name]=@name,[deleted]=@deleted,[sigle]=@sigle,[small_name]=@small_name,
                [volunteer_count]=@volunteer_count,[agrement_solidarity]=@agrement_solidarity,[employee_count]=@employee_count,
                [siret]=@siret,[insertionType]=@insertionType,[activity_id]=@activity_id,[fiscal_status]=@fiscalStatus,[registre]=@registre,
                [legalForm]=@legalForm,[agrement_date]=@agrement_date,[date_create]=@date_create WHERE [id] = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransac.Connection, sqlTransac))
            {
                c.AddParam("@id", body.Id);
                c.AddParam("@name", body.Name);
                c.AddParam("@deleted", body.IsDeleted);

                c.AddParam("@fiscalStatus", body.FiscalStatus);
                c.AddParam("@registre", body.Registre);

                c.AddParam("@sigle", body.Sigle);
                c.AddParam("@small_name", body.SmallName);
                c.AddParam("@volunteer_count", body.VolunteerCount);

                c.AddParam("@agrement_solidarity", body.AgrementSolidarity);
                c.AddParam("@employee_count", body.EmployeeCount);
                c.AddParam("@siret", body.Siret);
                c.AddParam("@legalForm", body.LegalForm);
                c.AddParam("@insertionType", body.InsertionType);

                if (body.Activity != null)
                    c.AddParam("@activity_id", body.Activity.Id);
                else
                    c.AddParam("@activity_id", null);

                c.AddParam("@agrement_date", body.AgrementDate != DateTime.MinValue ? body.AgrementDate : null);
                c.AddParam("@date_create"
                           , body.CreationDate != DateTime.MinValue ? (object) body.RegistrationDate : null
                    );

                c.ExecuteNonQuery();
            }
        }

        public Corporate SelectBodyCorporateById(int id)
        {
            int? activityId = null;
            int? districtId = null;
            int? secondaryDistrictId = null;

            Corporate body = null;
            const string q = @" SELECT * FROM Tiers 
            INNER JOIN Corporates ON Tiers.id = Corporates.id
            WHERE [deleted] = 0 AND Tiers.id=@id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@id", id);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null && !r.Empty)
                    {
                        r.Read();
                        body = new Corporate
                                   {
                                       Id = r.GetInt("id"),
                                       Name = r.GetString("name"),
                                       Status = (OClientStatus) r.GetSmallInt("status"),
                                       IsDeleted = r.GetBool("deleted"),
                                       CashReceiptIn = r.GetNullInt("cash_input_voucher_number"),
                                       CashReceiptOut =
                                           r.GetNullInt("cash_output_voucher_number"),
                                       Type = r.GetChar("client_type_code") == 'I'
                                                  ? OClientTypes.Person
                                                  : r.GetChar("client_type_code") == 'G'
                                                        ? OClientTypes.Group
                                                        : OClientTypes.Corporate,
                                       Scoring = r.GetNullDouble("scoring"),
                                       LoanCycle = r.GetInt("loan_cycle"),
                                       Active = r.GetBool("active"),
                                       BadClient = r.GetBool("bad_client"),
                                       OtherOrgName = r.GetString("other_org_name"),
                                       OtherOrgAmount = r.GetMoney("other_org_amount"),
                                       OtherOrgDebts = r.GetMoney("other_org_debts"),
                                       City = r.GetString("city"),
                                       Address = r.GetString("address"),
                                       SecondaryCity = r.GetString("secondary_city"),
                                       SecondaryAddress = r.GetString("secondary_address"),
                                       HomePhone = r.GetString("home_phone"),
                                       PersonalPhone = r.GetString("personal_phone"),
                                       SecondaryHomePhone = r.GetString("secondary_home_phone"),
                                       SecondaryPersonalPhone = r.GetString("secondary_personal_phone"),
                                       VolunteerCount = r.GetNullInt("volunteer_count"),
                                       EmployeeCount = r.GetNullInt("employee_count"),
                                       Sigle = r.GetString("sigle"),
                                       Siret = r.GetString("siret"),
                                       SmallName = r.GetString("small_name"),
                                       CreationDate = r.GetDateTime("creation_date"),
                                       AgrementDate = r.GetNullDateTime("agrement_date"),
                                       FollowUpComment = r.GetString("follow_up_comment"),
                                       HomeType = r.GetString("home_type"),
                                       SecondaryHomeType = r.GetString("secondary_hometype"),
                                       ZipCode = r.GetString("zipCode"),
                                       SecondaryZipCode = r.GetString("secondary_zipCode"),
                                       Sponsor1 = r.GetString("sponsor1"),
                                       Sponsor2 = r.GetString("sponsor2"),
                                       Sponsor1Comment = r.GetString("sponsor1_Comment"),
                                       Sponsor2Comment = r.GetString("sponsor2_comment"),
                                       FiscalStatus = r.GetString("fiscal_status"),
                                       Registre = r.GetString("registre"),
                                       LegalForm = r.GetString("legalForm"),
                                       InsertionType = r.GetString("insertionType"),
                                       Email = r.GetString("e_mail"),
                                       FavouriteLoanOfficerId = r.GetNullInt("loan_officer_id"),
                                       Branch = new Branch {Id = r.GetInt("branch_id")},
                                       RegistrationDate = r.GetNullDateTime("date_create") ?? TimeProvider.Today
                                   };

                        districtId = r.GetNullInt("district_id");
                        secondaryDistrictId = r.GetNullInt("secondary_district_id");
                        activityId = r.GetNullInt("activity_id");
                    }
                }
            }
            if (body!=null)
            {
                if (body.FavouriteLoanOfficerId != null)
                {
                    UserManager userManager = new UserManager(User.CurrentUser);
                    body.FavouriteLoanOfficer = userManager.SelectUser((int)body.FavouriteLoanOfficerId, true);
                }
            }
                
            if (districtId.HasValue) body.District = _locations.SelectDistrictById(districtId.Value);

            if (secondaryDistrictId.HasValue) body.SecondaryDistrict = _locations.SelectDistrictById(secondaryDistrictId.Value);

            if (activityId.HasValue) body.Activity = _doam.SelectEconomicActivity(activityId.Value);

            if (body != null)
            {
                if (_projectManager != null)
                    body.AddProjects(_projectManager.SelectProjectsByClientId(body.Id));
                if (_savingManager != null)
                    body.AddSavings(_savingManager.SelectSavings(body.Id));
            }


            // Selecting Corporate contacts - Not very logical, but asked by Julien M.
            const string sqlContactText = @" 
                                 SELECT Persons.id, 
                                        Persons.first_name,
                                        Persons.last_name,
                                        Tiers.personal_phone,
                                        CorporatePersonBelonging.position 
                                 FROM Tiers
                                 INNER JOIN CorporatePersonBelonging ON Tiers.id = CorporatePersonBelonging.person_id 
                                 INNER JOIN Persons ON Tiers.id = Persons.id 
                                 WHERE CorporatePersonBelonging.corporate_id = @id";

            Person person = null;
            Contact contact = null;
            List<Contact> contacts = new List<Contact>();

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(sqlContactText, conn))
            {
                c.AddParam("@id", id);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null && !r.Empty)
                    {
                        while (r.Read())
                        {
                            person = new Person()
                                         {
                                             Id = r.GetInt("id"),
                                             FirstName = r.GetString("first_name"),
                                             LastName = r.GetString("last_name"),
                                             PersonalPhone = r.GetString("personal_phone")
                                         };

                            contact = new Contact()
                                          {
                                              Tiers = person,
                                              Position = r.GetString("position")
                                          };
                            contacts.Add(contact);
                        }
                    }
                }
            }

            body.Contacts = contacts;

            OnClientSelected(body);

            return body;
        }

        public List<ContactRole> SelectRoleAll()
        {
            var liste = new List<ContactRole>();
            var q = @"SELECT * FROM Roles";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                while (r.Read())
                {
                    var role = new ContactRole
                    {
                        Id = r.GetInt("id"),
                        Name = r.GetString("code")
                    };

                    liste.Add(role);
                }
            }
            return liste;
        }

        public void UpdateClientStatus(IClient pClient, SqlTransaction sqlTransaction)
        {
            const string q = "UPDATE [Tiers] SET [status] = @status, [active] = @active WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, sqlTransaction.Connection, sqlTransaction))
            {
                c.AddParam("@id", pClient.Id);
                c.AddParam("@status", (int)pClient.Status);

                if (pClient.Status == OClientStatus.Active || pClient.Status == OClientStatus.Bad)
                    c.AddParam("@active", true);
                else
                    c.AddParam("@active", false);

                c.ExecuteNonQuery();
            }
        }

        public int GetNumberOfGuarantorsLoansCover(int TiersID, SqlTransaction sqlTransaction)
        {
            const string q = @"SELECT COUNT(contract_id)
                                     FROM LinkGuarantorCredit lgc
                                     INNER JOIN Contracts c ON lgc.contract_id = c.id
                                     WHERE tiers_id = @id AND c.closed = 0";
            using (OpenCbsCommand c = new OpenCbsCommand(q,sqlTransaction.Connection, sqlTransaction))
            {
                c.AddParam("@id", TiersID);
                return Convert.ToInt32(c.ExecuteScalar());
            }
        }

        public void SetFavourLoanOfficerForPerson(int clientId, int loanOfficerId, SqlTransaction transaction)
        {
            const string q = @"UPDATE Persons
                                 SET [loan_officer_id]=@loan_officer_id
                                 WHERE Persons.id=@person_id";
            using (OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                c.AddParam("@person_id", clientId);
                c.AddParam("@loan_officer_id", loanOfficerId);
                c.ExecuteNonQuery();
            }
        }

        public void SetFavourLoanOfficerForGroup(int groupId, int loanOfficerId, SqlTransaction transaction)
        {
            const string q =
                                  @"UPDATE Groups
                                  SET loan_officer_id=@loan_officer_id
                                  WHERE Groups.id=@group_id";
            using (OpenCbsCommand c = new OpenCbsCommand(q, transaction.Connection, transaction))
            {
                c.AddParam("@group_id", groupId);
                c.AddParam("@loan_officer_id", loanOfficerId);
                c.ExecuteNonQuery();
            }
        }

        public void SetFavourLoanOfficerForCorporate(int corporateId, int loanOfficerId)
        {
            const string q =
                                  @"UPDATE Corporates
                                  SET loan_officer_id=@loan_officer_id
                                  WHERE Corporates.id=@corporate_id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@corporate_id", corporateId);
                c.AddParam("@loan_officer_id", loanOfficerId);
                c.ExecuteNonQuery();
            }
        }

        public void IncrementLoanCycleByContractId(int contractId, SqlTransaction t)
        {
            const string q = "UPDATE dbo.Tiers SET loan_cycle = loan_cycle + 1 " +
                "WHERE id = (" +
                "SELECT j.tiers_id FROM dbo.Projects AS j " +
                "LEFT JOIN dbo.Contracts AS c ON c.project_id = j.id " +
                "WHERE c.id = @contractId)";
            using (OpenCbsCommand c = new OpenCbsCommand(q, t.Connection, t))
            {
                c.AddParam("@contractId", contractId);
                c.ExecuteNonQuery();
            }

            const string query2 = "UPDATE dbo.Tiers SET loan_cycle = loan_cycle + 1 " +
                "WHERE id IN (" +
                "SELECT person_id FROM dbo.LoanShareAmounts WHERE contract_id = @contractId)";
            using (OpenCbsCommand cmd = new OpenCbsCommand(query2, t.Connection, t))
            {
                cmd.AddParam("@contractId", contractId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DecrementLoanCycleByContractId(int contractId, SqlTransaction t)
        {
            const string q = "UPDATE dbo.Tiers SET loan_cycle = loan_cycle - 1 " +
                "WHERE id = (" +
                "SELECT j.tiers_id FROM dbo.Projects AS j " +
                "LEFT JOIN dbo.Contracts AS c ON c.project_id = j.id " +
                "WHERE c.id = @contractId)";
            using (OpenCbsCommand c = new OpenCbsCommand(q, t.Connection, t))
            {
                c.AddParam("@contractId", contractId);
                c.ExecuteNonQuery();
            }

            const string query2 = "UPDATE dbo.Tiers SET loan_cycle = loan_cycle - 1 " +
                "WHERE id IN (" +
                "SELECT person_id FROM dbo.LoanShareAmounts WHERE contract_id = @contractId)";
            using (OpenCbsCommand cmd = new OpenCbsCommand(query2, t.Connection, t))
            {
                cmd.AddParam("@contractId", contractId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> SelectAllSetUpFields(string pFieldsType)
        {
            List<string> list = new List<string>();
            string q = string.Format("SELECT * FROM {0}", pFieldsType);
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r == null || r.Empty) return new List<string>();
                    while (r.Read())
                    {
                        list.Add(r.GetString("value"));
                    }
                }
            }
            return list;
        }

        public int AddVillage(Village pVillage, SqlTransaction pSqlTransac)
        {
            int id = AddTiers(pVillage, pSqlTransac);

            const string q = @"INSERT INTO [Villages]
                                    (
                                        [id],
                                        [name],
                                        [establishment_date],
                                        [loan_officer], 
                                        [meeting_day]
                                    ) 
                                     VALUES
                                    (
                                         @id,
                                         @name,
                                         @establishmentDate,
                                         @loan_officer,
                                         @meeting_day
                                    )";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@id", id);
                c.AddParam("@name", pVillage.Name);
                c.AddParam("@establishmentDate", pVillage.EstablishmentDate);
                c.AddParam("@loan_officer", pVillage.LoanOfficer.Id);
                if (pVillage.MeetingDay.HasValue)
                    c.AddParam("@meeting_day", (int) pVillage.MeetingDay);
                else
                    c.AddParam("@meeting_day", null);

                c.ExecuteNonQuery();
            }
            return id;
        }

        public void UpdateVillage(Village pVillage, SqlTransaction pSqlTransac)
        {
            UpdateTiers(pVillage, pSqlTransac);

            const string q = @"UPDATE [Villages] 
                                     SET [name]=@name, 
                                         [establishment_date] = @establishmentDate, 
                                         [loan_officer] = @loan_officer,
                                         [meeting_day] = @meeting_day 
                                     WHERE id = @id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                SetNonSolidaryGroup(pVillage, c);
                c.ExecuteNonQuery();
            }

            List<VillageMember> members = SelectVillageMembersByVillageId(pVillage.Id, pSqlTransac, null);
            
            foreach (VillageMember member in members)
            {
                DeleteVillageMember(pVillage.Id, member.Tiers.Id, pSqlTransac);
            }

            foreach (VillageMember member in pVillage.Members)
            {
                bool isNew = true;
                foreach (VillageMember member2 in members)
                {
                    if (member.Tiers.Id == member2.Tiers.Id)
                    {
                        isNew = false;
                        break;
                    }
                }

                if (isNew)
                    AddMemberToVillage(member, pVillage, (member.Tiers.Id == pVillage.Leader.Tiers.Id), pSqlTransac);
                else
                    UpdateMemberInVillage(member, pVillage, (member.Tiers.Id == pVillage.Leader.Tiers.Id), pSqlTransac);
            }
        }

        private void SetNonSolidaryGroup(Village pVillage, OpenCbsCommand c)
        {
            c.AddParam("@id", pVillage.Id);
            c.AddParam("@name", pVillage.Name);
            c.AddParam("@establishmentDate", pVillage.EstablishmentDate);
            c.AddParam("@loan_officer", pVillage.LoanOfficer.Id);
            if (pVillage.MeetingDay.HasValue)
                c.AddParam("@meeting_day", (int)pVillage.MeetingDay);
            else
                c.AddParam("@meeting_day", null);
        }

        private void AddMemberToVillage(VillageMember pMember, Village pVillage, bool leader, SqlTransaction pSqlTransac)
        {
            const string q = @"INSERT INTO VillagesPersons 
                                (
                                    village_id,
                                    person_id,
                                    joined_date,
                                    left_date,
                                    is_leader,
                                    currently_in
                                )
                            VALUES 
                                (
                                    @village_id,
                                    @person_id,
                                    @joined_date,
                                    @left_date,
                                    @isLeader,
                                    @currentlyIn
                                )";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@village_id", pVillage.Id);
                c.AddParam("@person_id", pMember.Tiers.Id);
                c.AddParam("@joined_date", TimeProvider.Now);
                c.AddParam("@left_date", null);
                c.AddParam("@isLeader", leader);
                c.AddParam("@currentlyIn", true);
                c.ExecuteNonQuery();
            }
        }

        private void UpdateMemberInVillage(VillageMember pMember, Village pVillage, bool leader, SqlTransaction pSqlTransac)
        {
            const string q = @"UPDATE VillagesPersons SET left_date = NULL,
                                is_leader = @isLeader,
                                currently_in = @currentlyIn
                                WHERE village_id = @village_id
                                AND person_id = @person_id";
            
            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@village_id", pVillage.Id);
                c.AddParam("@person_id", pMember.Tiers.Id);
                c.AddParam("@isLeader", leader);
                c.AddParam("@currentlyIn", true);
                c.ExecuteNonQuery();
            }
        }

        private static char _GetClientChar(OClientTypes clientType)
        {
            char type = 'I';
            switch (clientType)
                {
                    case OClientTypes.Person :
                        type = 'I';
                       break;

                    case OClientTypes.Group:
                       type = 'G';
                       break;
                    case OClientTypes.Corporate:
                       type = 'C';
                       break;
                    case OClientTypes.Village:
                       type = 'V';
                       break;
                }
            return type;
        }

        private int AddTiers(IClient pTiers, SqlTransaction pSqlTransac)
        {
            const string q = @"INSERT INTO [Tiers](
                                       [creation_date], 
                                       [client_type_code], 
                                       [scoring], 
                                       [loan_cycle], 
                                       [active], 
                                       [bad_client], 
                                       [other_org_name], 
                                       [other_org_amount], 
                                       [other_org_debts], 
                                       [district_id], 
                                       [city], 
                                       [address], 
                                       [secondary_district_id], 
                                       [secondary_city], 
                                       [secondary_address], 
                                       [cash_input_voucher_number], 
                                       [cash_output_voucher_number],
                                       [home_phone],
                                       [personal_phone],
                                       [secondary_home_phone],
                                       [secondary_personal_phone],
                                       [e_mail],
                                       [home_type],
                                       [secondary_e_mail],
                                       [secondary_homeType],
                                       [status],
                                       [other_org_comment],
                                       [sponsor1],
                                       [sponsor2],
                                       [sponsor1_comment],
                                       [sponsor2_comment],
                                       [follow_up_comment],
                                       [zipCode],
                                       [secondary_zipCode],
                                       [branch_id]) 
                                     VALUES(@creationDate, 
                                       @clientTypeCode, 
                                        @scoring, 
                                        @loanCycle, 
                                        @active, 
                                        @badClient, 
                                        @otherOrgName, 
                                        @otherOrgAmount, 
                                        @otherOrgDebts, 
                                        @districtId, 
                                        @city, 
                                        @address, 
                                        @secondaryDistrict, 
                                        @secondaryCity, 
                                        @secondaryAddress, 
                                        @cashIn, 
                                        @cashOut,
                                        @homePhone,
                                        @personalPhone,
                                        @secondaryHomePhone,
                                        @secondaryPersonalPhone,
                                        @Email,
                                        @HomeType,
                                        @SecondaryEmail,
                                        @SecondaryHometype,
                                        @status,    
                                        @OtherOrgComments, 
                                        @sponsor1,
                                        @sponsor2,
                                        @sponsor1Comment,
                                        @sponsor2Comment, 
                                        @followUpComment,
                                        @zipCode,
                                        @secondaryZipCode,
                                        @branchId) 
                                      SELECT SCOPE_IDENTITY()";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@creationDate", TimeProvider.Today);
                c.AddParam("@clientTypeCode", _GetClientChar(pTiers.Type));
                c.AddParam("@scoring", pTiers.Scoring);
                c.AddParam("@loanCycle", pTiers.LoanCycle);
                c.AddParam("@active", pTiers.Active);
                c.AddParam("@badClient", pTiers.BadClient);
                c.AddParam("@otherOrgName", pTiers.OtherOrgName);
                c.AddParam("@otherOrgAmount", pTiers.OtherOrgAmount);
                c.AddParam("@otherOrgDebts", pTiers.OtherOrgDebts);
                c.AddParam("@city", pTiers.City);
                c.AddParam("@address", pTiers.Address);
                c.AddParam("@secondaryCity", pTiers.SecondaryCity);
                c.AddParam("@secondaryAddress", pTiers.SecondaryAddress);
                c.AddParam("@cashIn", pTiers.CashReceiptIn);
                c.AddParam("@cashOut", pTiers.CashReceiptOut);
                c.AddParam("@homePhone", pTiers.HomePhone);
                c.AddParam("@personalPhone", pTiers.PersonalPhone);
                c.AddParam("@secondaryHomePhone", pTiers.SecondaryHomePhone);
                c.AddParam("@secondaryPersonalPhone", pTiers.SecondaryPersonalPhone);
                c.AddParam("@Email", pTiers.Email);
                c.AddParam("@secondaryEmail", pTiers.SecondaryEmail);
                c.AddParam("@HomeType", pTiers.HomeType);
                c.AddParam("@secondaryHomeType", pTiers.SecondaryHomeType);
                c.AddParam("@zipCode", pTiers.ZipCode);
                c.AddParam("@secondaryZipCode", pTiers.SecondaryZipCode);
                c.AddParam("@status", (int)pTiers.Status);
                c.AddParam("@OtherOrgComments", pTiers.OtherOrgComment);
                c.AddParam("@sponsor1", pTiers.Sponsor1);
                c.AddParam("@sponsor2", pTiers.Sponsor2);
                c.AddParam("@sponsor1Comment", pTiers.Sponsor1Comment);
                c.AddParam("@sponsor2Comment", pTiers.Sponsor2Comment);
                c.AddParam("@followUpComment", pTiers.FollowUpComment);
                c.AddParam("@branchId", pTiers.Branch.Id);

                if (pTiers.District != null)
                    c.AddParam("@districtId", pTiers.District.Id);
                else
                    c.AddParam("@districtId", null);

                if (pTiers.SecondaryDistrict != null)
                    c.AddParam("@secondaryDistrict", pTiers.SecondaryDistrict.Id);
                else
                    c.AddParam("@secondaryDistrict", null);

                int a = int.Parse(c.ExecuteScalar().ToString());
                return a;
            }
        }

        
        public void RestorMemberOfGroupByEventId(int pEventId, Loan pLoan, SqlTransaction pSqlTransac)
        {
            string q = @"SELECT person_id 
                              FROM LoanShareAmounts 
                              WHERE event_id = @event_id
                              AND contract_id = @contract_id";
            int personId;

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@event_id", pEventId);
                c.AddParam("@contract_id", pLoan.Id);

                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r != null && !r.Empty)
                    {
                        r.Read();
                        personId = r.GetInt("person_id");
                    }
                    else
                        personId = 0;
                }
            }

            q = @"UPDATE PersonGroupBelonging
                        SET currently_in = 1, 
                          left_date = NULL 
                        WHERE group_id  = @groupId 
                        AND person_id = @personId";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@groupId", pLoan.Project.Client.Id);
                c.AddParam("@personId", personId);
                c.ExecuteNonQuery();
            }

            q = @"UPDATE LoanShareAmounts 
                           SET payment_date = null,
                            event_id = null
                           WHERE person_id = @person_id
                             AND group_id = @group_id
                             AND contract_id = @contract_id";

            using (OpenCbsCommand c = new OpenCbsCommand(q, pSqlTransac.Connection, pSqlTransac))
            {
                c.AddParam("@payment_date", null);
                c.AddParam("@event_id", null);

                c.AddParam("@person_id", personId);
                c.AddParam("@group_id", pLoan.Project.Client.Id);
                c.AddParam("@contract_id", pLoan.Id);
                c.ExecuteNonQuery();
            }
        }

        private void OnClientSelected(IClient client)
        {
            if (ClientSelected != null)
            {
                ClientSelected(client);
            }
        }

		public Person SelectPersonByPassport(string passport)
        {
            const string q = @"SELECT id FROM dbo.Persons
            WHERE identification_data = @passport";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@passport", passport);
                object val = c.ExecuteScalar();
                return null == val ? null : SelectPersonById(Convert.ToInt32(val));
            }
        }

        public Group SelectGroupByName(string name)
        {
            const string q = @"SELECT id FROM dbo.Groups
            WHERE name COLLATE Latin1_General_CS_AS = @name";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@name", name);
                object val = c.ExecuteScalar();
                return null == val ? null : SelectGroupById(Convert.ToInt32(val));
            }
        }

		public int SelectClientIdByContractCode(string code)
        {
            const string q = @"select j.tiers_id
            from dbo.Contracts c
            inner join dbo.Projects j on j.id = c.project_id
            where c.contract_code = @code";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@code", code);
                object value = c.ExecuteScalar();
                return null == value ? 0 : Convert.ToInt32(value);
            }
        }

        public int GetPersonGroupCount(int personId)
        {
            const string query = "DECLARE @result INT " +
                                 "SELECT @result = COUNT(1) FROM PersonGroupBelonging WHERE person_id = @personId AND currently_in = 1" +
                                 "SELECT @result = @result + COUNT(1) FROM VillagesPersons WHERE person_id = @personId AND currently_in = 1" +
                                 "SELECT @result";
            using (var connection = GetConnection())
            using (var command = new OpenCbsCommand(query, connection))
            {
                command.AddParam("personId", personId);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}
