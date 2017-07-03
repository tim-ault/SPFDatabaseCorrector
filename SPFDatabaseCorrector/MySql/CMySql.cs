using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SPFDatabaseCorrector
{
    class CMySql
    {
        private string m_ConnectionString;
        MySqlConnection m_Connection;

        private List<string> m_ByteLookup;

        private const string SERVER = "10.10.200.20";
        private const string USER_ID = "root";
        private const string PASSWORD = "muppet";

        public CMySql(string database)
        {
            m_ConnectionString = CreateConnectionString(SERVER, database, USER_ID, PASSWORD);
            m_Connection = new MySqlConnection(m_ConnectionString);
            m_ByteLookup = CreateByteLookup();

        }

        /// <summary>
        /// Creates a lookup table for conversion of sensor data to a string for database storage.
        /// </summary>
        /// <returns>List of strings.</returns>
        private List<string> CreateByteLookup()
        {
            List<string> ByteLookup = new List<string>();
            for (int i = 0; i < 256; i++)
            {
                string ByteString = i.ToString("X2");
                ByteLookup.Add(ByteString);

            }
            return ByteLookup;
        }

        private string CreateConnectionString(string server, string database, string userId, string password)
        {
            MySqlConnectionStringBuilder myCSB = new MySqlConnectionStringBuilder();
            myCSB.Server = server;
            myCSB.Database = database;
            myCSB.UserID = userId;
            myCSB.Password = password;

            return myCSB.ConnectionString;
        }

        public List<int> GetCorruptDataIDs(string table, int faceType)
        {
            List<int> CorruptDataIDs = new List<int>();
            MySqlCommand Command = m_Connection.CreateCommand();
            MySqlDataReader myReader;

            try
            {
                m_Connection.Open();
                //Command.CommandText = string.Format(@"SELECT id FROM {0} WHERE Source = 4 AND Valid = 'False' AND Face_Type = {1} ", table, faceType);
                Command.CommandText = string.Format(@"SELECT id FROM {0} WHERE Source = 4 AND Face_Type = {1} ", table, faceType);

                myReader = Command.ExecuteReader();
                if (myReader.HasRows)
                {
                    while (myReader.Read())
                    {

                        CorruptDataIDs.Add(myReader.GetInt32("id"));
                    }
                }
                myReader.Close();
                m_Connection.Close();
            }

            catch (Exception e)
            {
                m_Connection.Close();
            }



            return CorruptDataIDs;
        }

        public byte[] GetSensorDataBlob(string table, int id)
        {
            MySqlCommand Command = m_Connection.CreateCommand();
            MySqlDataReader myReader;
            byte[] BlobData = new byte[0];

            try
            {

                Command.CommandText = string.Format(@"SELECT LENGTH(Note_Data) FROM {0} WHERE ID = {1}", table, id);
                m_Connection.Open();
                int BlobLength = Convert.ToInt32(Command.ExecuteScalar());
                BlobData = new byte[BlobLength];


                Command.CommandText = string.Format(@"SELECT * FROM {0} WHERE ID = {1}", table, id);
                myReader = Command.ExecuteReader();
                if (myReader.HasRows)
                {
                    myReader.Read();
                    myReader.GetBytes(myReader.GetOrdinal("Note_Data"), 0, BlobData, 0, BlobLength);
                }
                myReader.Close();
                m_Connection.Close();
            }

            catch (Exception e)
            {
                m_Connection.Close();
            }

            return BlobData;
        }

        public bool OverWriteSensorDataBlob(string table, int id, byte[] blobDataByteArray)
        {

            string BlobDataString = ConvertByteArrayToHexString(blobDataByteArray);
            MySqlCommand Command = m_Connection.CreateCommand();

            try
            {
                Command.CommandText = string.Format(@"UPDATE {0} SET Note_Data = {1}, CommentID = 1001 WHERE id = {2}" , table, BlobDataString,id );
                m_Connection.Open();
                Command.ExecuteNonQuery();
                m_Connection.Close();

            }
            catch (Exception e)
            {
                m_Connection.Close();
                return false;
            }
            m_Connection.Close();
            return true;

        }

        /// <summary>
        /// Converts a byte array to a string of non delimited hex.
        /// </summary>
        /// <param name="byteArray">Byte array input</param>
        /// <returns>A string of non delimited hex.</returns>
        private string ConvertByteArrayToHexString(byte[] byteArray)
        {
            StringBuilder SbHex = new StringBuilder((byteArray.Length + 1) * 2);
            SbHex.Append("0x");
            for (int i = 0; i < byteArray.Length; i++)
            {
                SbHex.Append(m_ByteLookup[byteArray[i]]);
            }


            return SbHex.ToString();
        }





    }
}
