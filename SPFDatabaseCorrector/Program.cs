using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SPFDatabaseCorrector
{

    public enum NoteFace
    {
        FUFF = 1,    // Face up, face first
        FUFL,       // Face up, face last
        FDFF,       // Face down, face first
        FDFL        // Face down, face last
    }

    class Program
    {
        static CMySql m_MySql;
        public const byte SSP_STX = 0x7F;
        public const byte SENSOR_DATA_LENGTH = 50;
        public const string RAW_DATA_PREFIX = "Raw_";
        public const string CORRECTED_DATA_PREFIX = "Corrected_";
        public const string FILE_EXTENSION = ".csv";


        static void Main(string[] args)
        {
            DateTime StartTime = DateTime.Now;
            string Database = args[0];
            string Table = args[1];
            int Face = int.Parse(args[2]);

            string FaceString = ((NoteFace)Face).ToString();

            StringBuilder SbRawDataFile = new StringBuilder(30);
            string RawDataFile;
           
            StringBuilder SbCorrectedDataFile = new StringBuilder(30);
            string CorrectedDataFile;

            SbRawDataFile.Append(RAW_DATA_PREFIX);
            SbRawDataFile.Append(Database);
            SbRawDataFile.Append(".");
            SbRawDataFile.Append(Table);
            SbRawDataFile.Append("_");
            SbRawDataFile.Append(FaceString);
            SbRawDataFile.Append(FILE_EXTENSION);
            RawDataFile = SbRawDataFile.ToString();

            SbCorrectedDataFile.Append(CORRECTED_DATA_PREFIX);
            SbCorrectedDataFile.Append(Database);
            SbCorrectedDataFile.Append(".");
            SbCorrectedDataFile.Append(Table);
            SbCorrectedDataFile.Append("_");
            SbCorrectedDataFile.Append(FaceString);
            SbCorrectedDataFile.Append(FILE_EXTENSION);
            CorrectedDataFile = SbCorrectedDataFile.ToString();

            if (File.Exists(RawDataFile))
            {
                File.Delete(RawDataFile);
            }

            if (File.Exists(CorrectedDataFile))
            {
                File.Delete(CorrectedDataFile);
            }


            m_MySql = new CMySql(Database);
            Console.WriteLine("Finding Records");
            List<int> IDs = m_MySql.GetCorruptDataIDs(Table, Face);
            Console.WriteLine(IDs.Count.ToString() + " Found");
            foreach (int ID in IDs)
            {
                Console.WriteLine("Processing data for ID " + ID.ToString());
                byte[] RawBlobData = m_MySql.GetSensorDataBlob(Table, ID);
                byte[] CorrectedBlobData = new byte[RawBlobData.Length];
                int BlobIndex = 0;
                while (BlobIndex < RawBlobData.Length)
                {
                    List<byte> SensorData = new List<byte>();
                    byte[] SensorDataBytes = new byte[SENSOR_DATA_LENGTH];
                    Array.Copy(RawBlobData, BlobIndex, SensorDataBytes, 0, SENSOR_DATA_LENGTH);
                    SensorData.AddRange(SensorDataBytes);
                    RemoveByteStuffing(SensorData);
                    SensorDataBytes = SensorData.ToArray();
                    Array.Copy(SensorDataBytes, 0, CorrectedBlobData, BlobIndex, SENSOR_DATA_LENGTH);
                    BlobIndex += SENSOR_DATA_LENGTH;
                }

                m_MySql.OverWriteSensorDataBlob(Table, ID, CorrectedBlobData);

                Console.WriteLine("Writing to CSV files");
                string RawCsvFileContents = ByteArrayToCsvPadded(RawBlobData);
                
                //Console.WriteLine("Writing to " + RawDataFile);
                File.AppendAllText(RawDataFile, RawCsvFileContents + Environment.NewLine);


                string CorrectedCsvFileContents = ByteArrayToCsvPadded(CorrectedBlobData);
                
                //Console.WriteLine("Writing to " + CorrectedDataFile);
                File.AppendAllText(CorrectedDataFile, CorrectedCsvFileContents + Environment.NewLine);


            }

            DateTime EndTime = DateTime.Now;
            TimeSpan ElapsedTime = EndTime - StartTime;

            Console.WriteLine(IDs.Count.ToString() + " Records processed in " + ElapsedTime);
            
        }


        static void RemoveByteStuffing(List<byte> sensorData)
        {
            for (int i = 1; i < sensorData.Count; i++)    // Loop through the list.
            {
                if (sensorData[i] == SSP_STX && sensorData[i - 1] == SSP_STX)    // If consecutive bytes match the stuffed byte.
                {
                    sensorData.RemoveAt(i);    // Remove one of them.
                    sensorData.Add(0x00);    // Add zero at end to keep list length constant.
                }
            }
        }

        public static string ByteArrayToCsvPadded(byte[] byteArray)
        {
            StringBuilder sbCsv = new StringBuilder(byteArray.Length * 5);
            sbCsv.Append(byteArray[0].ToString("000"));   // First element not preceded by ', ' 
            for (int i = 1; i < byteArray.Length; i++)
            {
                sbCsv.Append(", ");
                sbCsv.Append(byteArray[i].ToString("000"));

            }
            return sbCsv.ToString();

        }

        public static string ByteArrayToCsv(byte[] byteArray)
        {
            StringBuilder sbCsv = new StringBuilder(byteArray.Length * 5);
            sbCsv.Append(byteArray[0].ToString());   // First element not preceded by ', ' 
            for (int i = 1; i < byteArray.Length; i++)
            {
                sbCsv.Append(", ");
                sbCsv.Append(byteArray[i].ToString());

            }
            return sbCsv.ToString();

        }
    }
}
