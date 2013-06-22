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

using System.Collections.Generic;
using OpenCBS.CoreDomain;
using System.Data.SqlClient;
using OpenCBS.CoreDomain.Dashboard;
using OpenCBS.Shared;

namespace OpenCBS.Manager
{
    public class UserManager : Manager
    {
        //private readonly User _user = new User();

        public UserManager(User pUser)
            : base(pUser)
        {
            //_user = pUser; 
        }

        public UserManager(string testDb) : base(testDb) { }

        public UserManager(string testDb, User pUser)
            : base(testDb)
        {
            //_user = pUser;
        }

        public int AddUser(User pUser)
        {
            const string sqlText = @"INSERT INTO [Users] (
                                       [deleted], 
                                       [role_code], 
                                       [user_name], 
                                       [user_pass], 
                                       [first_name], 
                                       [last_name], 
                                       [mail], 
                                       [sex],
                                       [phone]) 
                                     VALUES(
                                       @deleted, 
                                       @roleCode, 
                                       @username, 
                                       @userpass, 
                                       @firstname,
                                       @lastname, 
                                       @mail, 
                                       @sex,
                                       @phone) 
                                     SELECT SCOPE_IDENTITY()";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand sqlCommand = new OpenCbsCommand(sqlText, conn))
            {
                sqlCommand.AddParam("@deleted", false);
                SetUser(sqlCommand, pUser);
                pUser.Id = int.Parse(sqlCommand.ExecuteScalar().ToString());
                SaveUsersRole(pUser.Id, pUser.UserRole.Id);
            }
            return pUser.Id;
        }

        public void UpdateUser(User pUser)
        {
            const string sqlText = @"UPDATE [Users] 
                                     SET [user_name] = @username, 
                                       [user_pass] = @userpass, 
                                       [role_code] = @roleCode, 
                                       [first_name] = @firstname, 
                                       [last_name] = @lastname, 
                                       [mail] = @mail, 
                                       [sex] = @sex,
                                       [phone] = @phone
                                     WHERE [id] = @userId";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand sqlCommand = new OpenCbsCommand(sqlText, conn))
            {
                sqlCommand.AddParam("@userId", pUser.Id);
                SetUser(sqlCommand, pUser);
                sqlCommand.ExecuteNonQuery();
                _UpdateUsersRole(pUser.Id, pUser.UserRole.Id);
            }
        }

        private void SaveUsersRole(int pUserId, int pRoleId)
        {
            const string sqlText = @"INSERT INTO [UserRole]([role_id], [user_id]) 
                                   VALUES(@role_id, @user_id)";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand sqlCommand = new OpenCbsCommand(sqlText, conn))
            {
                sqlCommand.AddParam("@role_id", pRoleId);
                sqlCommand.AddParam("@user_id", pUserId);
                sqlCommand.ExecuteScalar();
            }
        }

        private void _UpdateUsersRole(int pUserId, int pRoleId)
        {
            const string sqlText = @"UPDATE [UserRole] 
                                    SET [role_id] = @role_id
                                    WHERE [user_id] = @user_id";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand sqlCommand = new OpenCbsCommand(sqlText, conn))
            {
                sqlCommand.AddParam("@role_id", pRoleId);
                sqlCommand.AddParam("@user_id",  pUserId);
                sqlCommand.ExecuteScalar();
            }
        }

        private static User _GetUser(OpenCbsReader pReader)
        {
            User user = new User
                            {
                                Id = pReader.GetInt("user_id"),
                                UserName = pReader.GetString("user_name"),
                                FirstName = pReader.GetString("first_name"),
                                LastName = pReader.GetString("last_name"),
                                Mail = pReader.GetString("mail"),
                                IsDeleted = pReader.GetBool("deleted"),
                                HasContract = (pReader.GetInt("contract_count") != 0),
                                Sex = pReader.GetChar("sex"),
                                Phone = pReader.GetString("phone")
                            };
            user.SetRole(pReader.GetString("role_code"));
            
            user.UserRole = new Role
                            {
                                RoleName = pReader.GetString("role_name"),
                                Id = pReader.GetInt("role_id"),
                                IsRoleForLoan = pReader.GetBool("role_of_loan"),
                                IsRoleForSaving = pReader.GetBool("role_of_saving"),
                                IsRoleForTeller = pReader.GetBool("role_of_teller")
                            };      

            return user;
        }

        private static void SetUser(OpenCbsCommand sqlCommand, User pUser)
        {
            sqlCommand.AddParam("@username", pUser.UserName);
            sqlCommand.AddParam("@userpass", pUser.Password);
            sqlCommand.AddParam("@roleCode", pUser.UserRole.ToString());
            sqlCommand.AddParam("@firstname", pUser.FirstName);
            sqlCommand.AddParam("@lastname",  pUser.LastName);
            sqlCommand.AddParam("@mail", pUser.Mail);
            sqlCommand.AddParam("@sex", pUser.Sex);
            sqlCommand.AddParam("@phone", pUser.Phone);
        }

        public void DeleteUser(User pUser)
        {
            const string sqlText = "UPDATE [Users] SET deleted = 1 WHERE [id] = @userId";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand sqlCommand = new OpenCbsCommand(sqlText, conn))
            {
                sqlCommand.AddParam("@userId", pUser.Id);
                sqlCommand.ExecuteNonQuery();
            }
        }

        public User SelectUser(int pUserId,bool pIncludeDeletedUser)
        {
            const string selectUser = @"SELECT [Users].[id] as user_id, 
                                                   [user_name], 
                                                   [user_pass], 
                                                   [role_code], 
                                                   [first_name], 
                                                   [last_name], 
                                                   [mail],
                                                   [sex],
                                                   [phone],
                                                   [Users].[deleted], 
                                                   [Roles].[id] as role_id, 
                                                   [Roles].[code] AS role_name,
                                                   [Roles].[role_of_loan],
                                                   [Roles].[role_of_saving],
                                                   [Roles].[role_of_teller],
                                                   (SELECT COUNT(a.id) 
                                                   FROM  (SELECT Credit.id, loanofficer_id 
                                                          FROM Credit 
                                                          GROUP BY  Credit.id, loanofficer_id ) a 
                                                   WHERE a.loanofficer_id = Users.id) AS contract_count 
                                            FROM [Users] INNER JOIN UserRole on UserRole.user_id = Users.id
                                            INNER JOIN Roles ON Roles.id = UserRole.role_id
                                            WHERE 1 = 1 ";

            string sqlText = selectUser + @" AND [Users].[id] = @id ";
                             
            if (!pIncludeDeletedUser)
                sqlText += @" AND [Users].[deleted] = 0";

            sqlText += @" GROUP BY [Users].[id],
                                   [Users].[deleted],
                                   [user_name],
                                   [user_pass],
                                   [role_code],
                                   [first_name],
                                   [last_name],
                                   [mail],                                   
                                   [sex],
                                   [phone],                                   
                                   [Roles].id, 
                                   [Roles].code,
                                   [Roles].[role_of_loan],
                                   [Roles].[role_of_saving],
                                   [Roles].[role_of_teller]";

            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand sqlCommand = new OpenCbsCommand(sqlText, conn))
            {
                sqlCommand.AddParam("@id", pUserId);
                using (OpenCbsReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader != null)
                    {
                        if (!reader.Empty)
                        {
                            reader.Read();
                            return _GetUser(reader);
                        }
                    }
                }
                return null;
            }
        }

        public List<User> SelectAll()
        {
            const string q = @"SELECT 
                                 id, 
                                 deleted, 
                                 user_name, 
                                 first_name,
                                 last_name, 
                                 user_pass,
                                 mail, 
                                 sex,
                                 phone, 
                                (SELECT COUNT(*)
                                 FROM dbo.Credit 
                                 WHERE loanofficer_id = u.id) AS num_contracts
                             FROM dbo.Users AS u";

            List<User> users = new List<User>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (r.Empty) return users;

                while (r.Read())
                {
                    User u = new User
                    {
                        Id = r.GetInt("id"),
                        FirstName = r.GetString("first_name"),
                        LastName = r.GetString("last_name"),
                        IsDeleted = r.GetBool("deleted"),
                        UserName = r.GetString("user_name"),
                        Password = r.GetString("user_pass"),
                        Mail = r.GetString("mail"),
                        Sex = r.GetChar("sex"),
                        HasContract = r.GetInt("num_contracts") > 0
                    };
                    users.Add(u);
                }
            }
            return users;
        }

        public List<User> SellectAllWithoutTellerOfBranch(Branch branch, User user)
        {
            const string q = @"SELECT 
                                u.id, 
                                u.deleted, 
                                u.user_name, 
                                u.first_name,
                                u.last_name, 
                                u.user_pass,
                                u.mail, 
                                u.sex,
                                u.phone,
                                (SELECT COUNT(*)
                                FROM dbo.Credit 
                                WHERE loanofficer_id = u.id) AS num_contracts
                                FROM dbo.Users AS u
                                INNER JOIN dbo.UsersBranches ub ON ub.user_id = u.id
                                INNER JOIN UserRole ur ON ur.user_id = u .id
                                INNER JOIN Roles r ON r.id = ur.role_id
                                WHERE u.deleted = 0 AND r.role_of_teller = 1
                                AND (u.id NOT IN (SELECT user_id FROM Tellers WHERE deleted = 0) OR u.id = @user_id)
                                AND ub.branch_id = @branch_id AND u.id IN (SELECT @boss_id
								                                           UNION ALL
								                                           SELECT subordinate_id
								                                           FROM dbo.UsersSubordinates
								                                           WHERE user_id = @boss_id)";

            List<User> users = new List<User>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            {
                c.AddParam("@branch_id", branch.Id);
                c.AddParam("@boss_id", User.CurrentUser.Id);
                c.AddParam("@user_id", user == null ? 0 : user.Id);
                using (OpenCbsReader r = c.ExecuteReader())
                {
                    if (r.Empty) return users;

                    while (r.Read())
                    {
                        User u = new User
                                     {
                                         Id = r.GetInt("id"),
                                         FirstName = r.GetString("first_name"),
                                         LastName = r.GetString("last_name"),
                                         IsDeleted = r.GetBool("deleted"),
                                         UserName = r.GetString("user_name"),
                                         Password = r.GetString("user_pass"),
                                         Mail = r.GetString("mail"),
                                         Sex = r.GetChar("sex"),
                                         HasContract = r.GetInt("num_contracts") > 0
                                     };
                        users.Add(u);
                    }
                }
            }
            return users;
        }

        public Dictionary<int, List<int>> SelectSubordinateRel()
        {
            const string q = @"SELECT user_id, subordinate_id
                               FROM dbo.UsersSubordinates
                               ORDER BY user_id";

            Dictionary<int, List<int>> retval = new Dictionary<int, List<int>>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (r.Empty) return retval;

                int currentId = 0;
                while (r.Read())
                {
                    int userId = r.GetInt("user_id");
                    if (currentId != userId)
                    {
                        currentId = userId;
                        retval.Add(currentId, new List<int>());
                    }
                    retval[currentId].Add(r.GetInt("subordinate_id"));
                }
            }
            return retval;
        }

        public Dictionary<int, List<int>> SelectBranchRel()
        {
            const string q = @"SELECT user_id, branch_id
            FROM dbo.UsersBranches
            ORDER BY user_id";
            Dictionary<int, List<int>> retval = new Dictionary<int, List<int>>();
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(q, conn))
            using (OpenCbsReader r = c.ExecuteReader())
            {
                if (r.Empty) return retval;

                while (r.Read())
                {
                    int userId = r.GetInt("user_id");
                    if (!retval.ContainsKey(userId)) retval.Add(userId, new List<int>());
                    retval[userId].Add(r.GetInt("branch_id"));
                }
            }
            return retval;
        }

        private void SaveSubordinates(User user)
        {
            const string query = @"DELETE FROM dbo.UsersSubordinates
            WHERE user_id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(query, conn))
            {
                c.AddParam("id", user.Id);
                c.ExecuteNonQuery();

                if (0 == user.SubordinateCount) return;

                List<string> subIds = new List<string>();
                foreach (User sub in user.Subordinates)
                {
                    subIds.Add(sub.Id.ToString());
                }

                c.CommandText =
                    @"INSERT INTO dbo.UsersSubordinates
                             (user_id, subordinate_id)
                             SELECT @id, number 
                             FROM dbo.IntListToTable(@list)";
                c.AddParam("@list", string.Join(",", subIds.ToArray()));
                c.ExecuteNonQuery();
            }
        }

        private void SaveBranches(User user)
        {
            const string query = @"DELETE FROM dbo.UsersBranches
                                   WHERE user_id = @id";
            using (SqlConnection conn = GetConnection())
            using (OpenCbsCommand c = new OpenCbsCommand(query, conn))
            {
                c.AddParam("@id", user.Id);
                c.ExecuteNonQuery();

                if (0 == user.BranchCount) return;

                List<string> ids = new List<string>();
                foreach (Branch b in user.Branches)
                {
                    ids.Add(b.Id.ToString());
                }
                c.CommandText = @"INSERT INTO dbo.UsersBranches
                (user_id, branch_id)
                SELECT @id, number
                FROM dbo.IntListToTable(@list)";
                c.AddParam("@list", string.Join(",", ids.ToArray()));
                c.ExecuteNonQuery();
            }
        }

        public void Save(User user)
        {
            SaveSubordinates(user);
            SaveBranches(user);
        }

        public Dashboard GetDashboard()
        {
            var dashboard = new Dashboard();
            using (var connection = GetConnection())
            using (var command = new OpenCbsCommand("GetDashboard", connection).
                AsStoredProcedure().
                With("@date", TimeProvider.Today).
                With("@userId", User.CurrentUser.Id))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var portfolioLine = new PortfolioLine
                    {
                        Name = reader.GetString("name"),
                        Amount = reader.GetDecimal("amount"),
                        Quantity = reader.GetInt("quantity"),
                    };
                    dashboard.PortfolioLines.Add(portfolioLine);
                    var portfolioLineForTable = new PortfolioLineForTable
                    {
                        Name = portfolioLine.Name,
                        Amount = (int)(portfolioLine.Amount),
                        Quantity = portfolioLine.Quantity,
                    };
                    dashboard.PortfolioLinesForTable.Add(portfolioLineForTable);
                }

                reader.NextResult();
                while (reader.Read())
                {
                    var stat = new ActionStat
                    {
                        Date = reader.GetDateTime("date"),
                        AmountDisbursed = reader.GetDecimal("amount_disbursed"),
                        NumberDisbursed = reader.GetInt("number_disbursed"),
                        AmountRepaid = reader.GetDecimal("amount_repaid"),
                        NumberRepaid = reader.GetInt("number_repaid"),
                    };
                    dashboard.ActionStats.Add(stat);
                }
            }

            return dashboard;
        }
    }
}
