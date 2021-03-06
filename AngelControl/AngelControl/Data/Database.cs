﻿using AngelControl.Class;
using AngelControl.Data.Class;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelControl.Data {
    public class Database : IDisposable {

        public string lastErrorMeassage = "";
        private MySqlConnection connect;

        public delegate void MethodContainerDabase(byte status);
        public static event MethodContainerDabase OnChangeDatabase;

        public bool Open(string server, uint port, string userId, string password, string database) {
            lastErrorMeassage = "";
            MySqlConnectionStringBuilder mySqlConnectionBuilder = new MySqlConnectionStringBuilder() {
                Server = server,
                Port = port,
                UserID = userId,
                Password = password,
                Database = database,
            };
            if (connect != null && connect.State == System.Data.ConnectionState.Open) {
                Close();
            }
            connect = new MySqlConnection(mySqlConnectionBuilder.ConnectionString);
            try {
                connect.Open();
                OnChangeDatabase(1);
                return true;
            } catch (Exception err) {
                lastErrorMeassage =  err.Message;
                OnChangeDatabase(0);
                return false;
            }
        }

        public bool OpenSave() {
            return Open(Security.Encryption.DecryptString(Properties.Settings.Default.DatabaseServer),
                        uint.Parse(Security.Encryption.DecryptString(Properties.Settings.Default.DatabasePort)),
                        Security.Encryption.DecryptString(Properties.Settings.Default.DatabaseUserId),
                        Security.Encryption.DecryptString(Properties.Settings.Default.DatabasePassword),
                        Security.Encryption.DecryptString(Properties.Settings.Default.DatabaseName));
        }

        public void Close() {
            if (connect != null && connect.State == System.Data.ConnectionState.Open) {
                connect.Close();
                OnChangeDatabase(2);
            }
        }

        private bool disposed = false;

        // реализация интерфейса IDisposable.
        public void Dispose() {
            Dispose(true);
            // подавляем финализацию
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    if (connect != null && connect.State == System.Data.ConnectionState.Open) {
                        Close();
                        connect.Dispose();
                    }
                }
                // освобождаем неуправляемые объекты
                disposed = true;
            }
        }

        // Деструктор
        ~Database() {
            Dispose(false);
        }

        //Selects////////////////////////////////////////////////////////////////////////////////////// 
        public Reg GetRegByNumcard (string whereNumcard) {
            Reg reg = null;
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = $@"SELECT id, lname, fname, pname FROM reg WHERE numcard = '{whereNumcard}'";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    reader.Read();
                    reg = new Data.Class.Reg() {
                        Id = reader.GetInt32(0),
                        Numcard = whereNumcard,
                        Lname = reader.GetValue(1).ToString(),
                        Fname = reader.GetValue(2).ToString(),
                        Pname = reader.GetValue(3).ToString(),
                    };
                }
            }
            return reg;
        }

        public List<Reg> GetRegs(Reg selectParameters) {
            string where = "";
            if (selectParameters.Lname != null) {
                where += $@" UPPER(r.lname) LIKE '{selectParameters.Lname.ToUpper()}%'";
            }
            if (selectParameters.Fname != null) {
                if (where.Length > 0) where += " AND";
                where += $@" UPPER(r.fname) LIKE '{selectParameters.Fname.ToUpper()}%'";
            }
            if (selectParameters.Pname != null) {
                if (where.Length > 0) where += " AND";
                where += $@" UPPER(r.pname) LIKE '{selectParameters.Pname.ToUpper()}%'";
            }
            if (selectParameters.Country != null) {
                if (where.Length > 0) where += " AND";
                where += $@" UPPER(r.country) LIKE '%{selectParameters.Country.ToUpper()}%'";
            }
            if (selectParameters.City != null) {
                if (where.Length > 0) where += " AND";
                where += $@" UPPER(r.city) LIKE '%{selectParameters.City.ToUpper()}%'";
            }
            if (selectParameters.Phone != null) {
                if (where.Length > 0) where += " AND";
                where += $@" REPLACE(REPLACE(REPLACE(r.phone,'-',''),'(',''),')','') LIKE '%{selectParameters.Phone.ToUpper()}%'";
            }
            if (selectParameters.StayWhere != null) {
                if (where.Length > 0) where += " AND";
                where += $@" sw.name LIKE '{selectParameters.StayWhere}'";
            }
            if (selectParameters.StayLength != null) {
                if (where.Length > 0) where += " AND";
                where += $@" sl.name LIKE '{selectParameters.StayLength}'";
            }
            if (where.Length > 0) where = " WHERE " + where;

            List<Reg> regs = new List<Reg>();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = $@"SELECT r.id, r.numcard, r.lname, r.fname, r.pname, r.phone, r.birthday, sw.id, sw.name, sl.id, sl.name, r.country, r.city, n.id, n.name" + 
                " FROM reg r" +
                " LEFT JOIN stay_where sw ON r.stay_where_id = sw.id" +
                " LEFT JOIN stay_length sl ON r.stay_length_id = sl.id" +
                " LEFT JOIN nutrition n ON r.nutrition_id = n.id" +
                where +
                " ORDER BY r.id";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    while (reader.Read()) {
                        Reg reg = new Reg() {
                            Id = reader.GetInt32(0),
                            Numcard = reader.GetValue(1).ToString(),
                            Lname = reader.GetValue(2).ToString(),
                            Fname = reader.GetValue(3).ToString(),
                            Pname = reader.GetValue(4).ToString(),
                            Phone = reader.GetValue(5).ToString(),
                            Birthday = reader.GetValue(6) != DBNull.Value ? reader.GetDateTime(6) : (DateTime?)null,
                            Age = reader.GetValue(6) != DBNull.Value ? Reg.GetAge(reader.GetDateTime(6)) : null,
                            StayWhereId = reader.GetValue(7) != DBNull.Value ? reader.GetInt32(7) : 0,
                            StayWhere = reader.GetValue(8).ToString(),
                            StayLengthId = reader.GetValue(9) != DBNull.Value ? reader.GetInt32(7) : 0,
                            StayLength = reader.GetValue(10).ToString(),
                            Country = reader.GetValue(11).ToString(),
                            City = reader.GetValue(12).ToString(),
                            NutritionId = reader.GetValue(13) != DBNull.Value ? reader.GetInt32(7) : 0,
                            Nutrition = reader.GetValue(14).ToString(),
                        };
                        regs.Add(reg);
                    }
                }
            }
            return regs;
        }

        public int CountRegs() {
            int count = 0;
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = $@"SELECT COUNT(id) FROM reg";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    reader.Read();
                    count = reader.GetInt32(0);
                }
            }
            return count;
        }

        public List<StayWhere> GetStayWheres() {
            List<StayWhere> stayWheres = new List<StayWhere>();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = "SELECT sw.id, sw.name" +
                " FROM stay_where sw" +
                " ORDER BY sw.id";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    while (reader.Read()) {
                        StayWhere stayWhere = new StayWhere() {
                            Id = reader.GetInt32(0),
                            Name = reader.GetValue(1).ToString(),
                        };
                        stayWheres.Add(stayWhere);
                    }
                }
            }
            return stayWheres;
        }
        public StayWhere GetStayWhere(string name) {
            StayWhere stayWhere = null;
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = "SELECT sw.id, sw.name" +
                " FROM stay_where sw" +
                $@" WHERE sw.name = '{name}'" +
                " ORDER BY sw.id";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    reader.Read();
                    stayWhere = new StayWhere() {
                        Id = reader.GetInt32(0),
                        Name = reader.GetValue(1).ToString(),
                    };
                }
            }
            return stayWhere;
        }

        public List<StayLength> GetStayLengths() {
            List<StayLength> stayLengths = new List<StayLength>();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = "SELECT sl.id, sl.name" +
                " FROM stay_length sl" +
                " ORDER BY sl.id";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    while (reader.Read()) {
                        StayLength stayLength = new StayLength() {
                            Id = reader.GetInt32(0),
                            Name = reader.GetValue(1).ToString(),
                        };
                        stayLengths.Add(stayLength);
                    }
                }
            }
            return stayLengths;
        }
        public StayLength GetStayLength(string name) {
            StayLength stayLength = null;
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = "SELECT sl.id, sl.name" +
                " FROM stay_length sl" +
                $@" WHERE sl.name = '{name}'" +
                " ORDER BY sl.id";
            using (DbDataReader reader = cmd.ExecuteReader()) {
                if (reader.HasRows) {
                    reader.Read();
                    stayLength = new StayLength() {
                        Id = reader.GetInt32(0),
                        Name = reader.GetValue(1).ToString(),
                    };
                }
            }
            return stayLength;
        }

        public DbDataReader QueryEmployee(string sql) {//Select * from reg
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = connect;
            cmd.CommandText = sql;
            return cmd.ExecuteReader();
        }
    }
}
