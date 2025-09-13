using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// FileIO class that allows the saving to and freading from documents created in or out of project

namespace IdleCollector
{
    public static class FileIO
    {
        /// <summary>
        /// Initializes file path for writing, useful for created save files
        /// </summary>
        /// <param name="name">Name of document</param>
        public static void CreateTxtFile(string name, string folder)
        {
            string stream = String.Format("../../../Content/{0}/{1}.txt", folder, name);

            StreamWriter fileWriter = new StreamWriter(stream);

            fileWriter.Close();
        }

        /// <summary>
        /// Overwrites document (string data)
        /// </summary>
        /// <param name="name">Name of document</param>
        /// <param name="data">List of strings</param>
        public static void WriteTo(string name, string folder, List<string> data)
        {
            string stream = String.Format("../../../Content/{0}/{1}.txt", folder, name);

            try
            {
                StreamWriter fileWriter = new StreamWriter(stream, false);

                foreach (string item in data)
                {
                    fileWriter.WriteLine(item);
                }

                fileWriter.Close();
            }
            catch
            {
                // This catch is specifically for the problem that I kept having with it not closing a document from what I assume is
                // too-fast of reading and writing and then opening before it can close, I haven't experienced any loss of save data since
                // implementing this, so I see it as a win
            }
        }

        /// <summary>
        /// Writes to the end of a document
        /// </summary>
        /// <param name="name">Name of document</param>
        /// <param name="data">Data being written to document</param>
        public static void AppendTo(string name, string folder, List<string> data)
        {
            string stream = String.Format("../../../Content/{0}/{1}.txt", folder, name);

            StreamWriter fileWriter = new StreamWriter(stream, true);

            foreach (string item in data)
            {
                fileWriter.WriteLine(item);
            }

            fileWriter.Close();
        }

        /// <summary>
        /// Overwrites a document entirely (floats only)
        /// </summary>
        /// <param name="name">Name of document</param>
        /// <param name="data">Float Data being written to document</param>
        public static void WriteTo(string name, string folder, List<float> data)
        {
            string stream = String.Format("../../../Content/{0}/{1}.txt", folder, name);

            StreamWriter fileWriter = new StreamWriter(stream);

            foreach (float item in data)
            {
                fileWriter.WriteLine(item);
            }

            fileWriter.Close();
        }

        /// <summary>
        /// Returns a list of string information from document 
        /// </summary>
        /// <param name="name">Name of document</param>
        /// <returns></returns>
        public static List<string> ReadFrom(string name, string folder)
        {
            string stream = String.Format("../../../Content/{0}/{1}.txt", folder, name);

            return ReadFrom(stream);
        }

        /// <summary>
        /// Returns a list of string information from document 
        /// </summary>
        /// <param name="name">Name of document</param>
        /// <returns></returns>
        public static List<string> ReadFrom(string path)
        {
            List<string> data = new List<string>();
            string stream = path;

            StreamReader fileReader = new StreamReader(stream);

            string line = null;

            while ((line = fileReader.ReadLine()) != null)
            {
                data.Add(line);
            }

            if (fileReader != null)
            {
                fileReader.Close();
            }

            return data;
        }

        /// <summary>
        /// Returns a documents information that is trusted to be all floats
        /// </summary>
        /// <param name="name">name of document</param>
        /// <returns></returns>
        public static List<float> NumReadFrom(string name, string folder)
        {
            List<float> data = new List<float>();
            string stream = String.Format("../../../Content/{0}/{1}.txt", folder, name);

            StreamReader fileReader = new StreamReader(stream);

            string line = null;

            while ((line = fileReader.ReadLine()) != null)
            {
                data.Add(float.Parse(line));
            }

            if (fileReader != null)
            {
                fileReader.Close();
            }

            return data;
        }

        public static void LoadDataInto<T>(T instance, List<string> data)
        {
            Type type = typeof(T);

            for (int i = 0; i < data.Count; i++)
            {
                string[] splitLine = data[i].Split(":");
                try
                {
                    FieldInfo field = type.GetField(splitLine[0], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    field.SetValue(instance, Convert.ChangeType(splitLine[1].Trim(), field.FieldType));
                }
                catch { }
            }
        }
    }
}
