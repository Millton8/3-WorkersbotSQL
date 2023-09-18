using kpworkersbot;
using Npgsql;
using System.Configuration;


namespace kpworkersbotsql
{
    internal static class sqlRepo
    {
        private static string connectionString = ConfigurationManager.AppSettings.Get("connectToDB");
        public static async Task<List<WorkerSalary>> FastSelectAsync()
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();
                var listSalary = new List<WorkerSalary>();
                var dataForFastReport = DateTime.Today.AddDays(-4).ToString("dd.MM.yy");
                var sql = $"SELECT uniq,name, SUM(salary) FROM rezofwork WHERE tbegin>'{dataForFastReport}' GROUP BY uniq,name;";
                using var cmd = new NpgsqlCommand(sql, con);
                using NpgsqlDataReader? rdr = cmd.ExecuteReader();


                while (rdr.Read())

                {
                    listSalary.Add(new WorkerSalary { Id = rdr.IsDBNull(0) ? "Нет идентификатора" : rdr.GetValue(0).ToString(), name = rdr.IsDBNull(1) ? "Нет имени" : rdr.GetValue(1).ToString(), salary = rdr.IsDBNull(2) ? "Нет зарплаты" : rdr.GetValue(2).ToString() });
                }
                foreach (var w in listSalary)
                    await Console.Out.WriteLineAsync(w.Id + "\t" + w.name + "\t" + w.salary);

                con.Close();
                return listSalary;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошипка\n" + ex);
                return null;
            }


        }
        public static async Task<List<WorkerSalary>> SelectBetweenTwoDatesAsync(string date)
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();
                var listSalary = new List<WorkerSalary>();
                bool isTwoDate = false;
                string[] twoDatesString = null;
                string sql = null;

                foreach (var c in date)
                {
                    if (c == ' ')
                        isTwoDate = true;
                }
                if (isTwoDate)
                {
                    Console.WriteLine("Две даты");
                    twoDatesString = date.Split(' ');
                    sql = $"SELECT uniq,name, SUM(salary) FROM rezofwork WHERE tbegin>='{twoDatesString[0]}' AND tbegin<'{twoDatesString[1]}' GROUP BY uniq,name;";
                }
                else
                    sql = $"SELECT uniq,name, SUM(salary) FROM rezofwork WHERE tbegin>='{date}' GROUP BY uniq,name;";


                using var cmd = new NpgsqlCommand(sql, con);
                using NpgsqlDataReader? rdr = cmd.ExecuteReader();
                Console.WriteLine(sql);

                while (rdr.Read())
                {
                    listSalary.Add(new WorkerSalary { Id = rdr.IsDBNull(0) ? "Нет идентификатора" : rdr.GetValue(0).ToString(), name = rdr.IsDBNull(1) ? "Нет имени" : rdr.GetValue(1).ToString(), salary = rdr.IsDBNull(2) ? "Нет зарплаты" : rdr.GetValue(2).ToString() });
                }

                con.Close();
                return listSalary;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошипка\n" + ex);
                return null;
            }


        }
        public static async Task InsertInDBAsync(WorkRezult workRezult)
        {
            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();
 
                var sqlInsert = $"INSERT INTO rezofwork VALUES (DEFAULT,@ID,@name,@project,@tBegin,@tEnd,@hourOfWorkFormat,@pricePerHour,@salary);";

                await using (var cmdInsert = new NpgsqlCommand(sqlInsert, con)) {

                    cmdInsert.Parameters.AddWithValue("ID", long.Parse(workRezult.ID));
                    cmdInsert.Parameters.AddWithValue("name", workRezult.name);
                    cmdInsert.Parameters.AddWithValue("project", workRezult.project);
                    cmdInsert.Parameters.AddWithValue("tBegin", workRezult.tBegin);
                    cmdInsert.Parameters.AddWithValue("tEnd", workRezult.tEnd);
                    cmdInsert.Parameters.AddWithValue("hourOfWorkFormat", workRezult.timeOfWork);
                    cmdInsert.Parameters.AddWithValue("pricePerHour", workRezult.pricePerHour);
                    cmdInsert.Parameters.AddWithValue("salary", workRezult.salary);


                    cmdInsert.ExecuteNonQuery();
                }
                
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошипка3\n" + ex);
            }


        }
    }
}
