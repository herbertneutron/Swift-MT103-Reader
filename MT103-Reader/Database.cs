using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT103_Reader {

         public class mt103acknew_files
        {
            public int FILE_NAME { get; set; }  
            public DateTime create_dt{ get; set; }
            public DateTime import_dt { get; set; }
            public int no_of_file { get; set; }
        }

    class Database {


        static internal async Task<Tuple<string, account_details_model>> getAccountDetails(string account_no )
        {

            account_details_model model = new account_details_model();
            string connstring = getConnectionString("PhoenixConn");           
            AseConnection conn = new AseConnection(connstring);

            string currDate = DateTime.Now.ToString("yyyyMMdd");

            try
            {
                string queryString =
                            "SELECT c.last_name + ' ' + c.first_name AS account_name " +
                            " , d.name_1 As branch_name " +
                            " , a.status AS account_status " +
                            " , b.name As rsm_name " +
                            " , f.staff_id as rsm_employee_number " +
                            " , d.branch_no " +
                            " , c.rim_no " +
                            " , a.rsm_id " +
                            " FROM " +
                                " phoenix..dp_acct a " +
                                " , phoenix..ad_gb_rsm b " +
                                " , phoenix..rm_acct c " +
                                " , phoenix..ad_gb_branch d " +
                                " , zib_applications_users f  " +
                            " WHERE a.acct_no = '" + account_no + "' " +
                            " AND LTRIM(RTRIM(a.status)) = 'Active' " +
                            " AND a.rsm_id = b.employee_id  " +
                            " AND a.rim_no = c.rim_no " +
                            " AND a.branch_no = d.branch_no  " +
                            " AND b.user_name = f.user_id";

                AseCommand command = new AseCommand(queryString, conn);
                AseDataReader reader;

                try {
                    conn.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read()) {
                        model.account_name          = reader["account_name"].ToString().Trim();
                        model.account_no            = account_no;
                        model.account_status        = reader["account_status"].ToString().Trim();
                        model.branch_code           = int.Parse(reader["branch_no"].ToString().Trim()).ToString("D3");
                        model.branch_name           = reader["branch_name"].ToString().Trim();
                        model.rim_no                = reader["rim_no"].ToString().Trim();
                        model.rsm_name              = reader["rsm_name"].ToString().Trim();
                        model.rsm_id                = reader["rsm_id"].ToString().Trim();
                        model.rsm_employee_number   = reader["rsm_employee_number"].ToString().Trim();
                    }
                } catch (Exception ex) {
                    return await Task.FromResult(Tuple.Create("Invalid account provided", new account_details_model()));
                } finally {
                    conn.Close();
                }
            } catch (Exception ex) {
                return await Task.FromResult(Tuple.Create("Invalid account provided", new account_details_model()));
            } finally {
                conn.Close();
            }

            if (!string.IsNullOrEmpty(model.account_name)) {
                return await Task.FromResult(Tuple.Create("", model));
            } else {
                return await Task.FromResult(Tuple.Create("Invalid account - " + account_no + " or account is not active.", model));
            }
        }


    }
}
