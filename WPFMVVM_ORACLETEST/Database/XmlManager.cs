using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WPFMVVM_ORACLETEST.Database
{
    public class XmlManager : Singleton<XmlManager>
    {
        #region " Properties & Variables " 
        private static string configFile = @"Config.xml"; 
        public static string ConfigFile 
        { 
            get { return configFile; } 
            set { configFile = value; } 
        } 
        #endregion " Properties & Variables End" 

        #region " Public methode " 
        public static string GetValue(params string[] args) 
        { 
            string result = string.Empty; 
            try 
            { 
                XDocument xDoc = XDocument.Load(configFile); 
                result = GetNodeValue(xDoc.FirstNode as XElement, 0, args); 
            } 
            catch (Exception ex) 
            { 
                result = ex.Message; 
                result = string.Empty; 
            } 
            return result; 
        } 
        public static bool SetValue(params string[] args) 
        { 
            bool result = false; 
            try 
            { 
                XDocument xDoc = XDocument.Load(configFile); 
                result = SetNodeValue(xDoc.FirstNode as XElement, 0, args); 
                xDoc.Save(configFile); 
            } 
            catch 
            { 
                result = false; 
            } 
            return result; 
        } 
        #endregion " Public methode End"
        #region " Private methode " 
        private static string GetNodeValue(XElement node, int idx, params string[] args) 
        { 
            string result = string.Empty; 
            if (args.Length > idx + 1) result = GetNodeValue(node.Element(args[idx]), ++idx, args); 
            else result = node.Element(args[idx]).Value.ToString(); 
            return result; 
        } 
        private static bool SetNodeValue(XElement node, int idx, params string[] args) 
        { 
            if (args.Length > idx + 1) 
            { 
                SetNodeValue(node.Element(args[idx]), ++idx, args); 
            } 
            else 
            { 
                node.SetValue(args[idx]); 
            } 
            return true; 
        } 
        #endregion " Private methode End" 
    }
}
