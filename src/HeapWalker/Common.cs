/* File: Common.cs
   Framework version:  .NET Framework version 3.5
   C# compiler: Microsoft (R) Visual C# 2008 Compiler version 3.5.21022.8
   Creation date: July 21st, 2008
   Developer: Angel J. Hernández
   e-m@ail: angeljesus14@hotmail.com
   Blog: http://msmvps.com/blogs/angelhernandez
   Description: Common functions used on HeapWalker
*/

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Threading;
using System.IO;
using System.Reflection;
using Winform = System.Windows.Forms;
using Win32Support;

namespace HeapWalker {
	/// <summary>
	/// 
	/// </summary>
	class Common {
		/// <summary>
		/// Extracts the image from assembly.
		/// </summary>
		/// <param name="resourceName">Name of the resource.</param>
		/// <param name="fileName">Name of the file.</param>
		public static void ExtractImageFromAssembly(string resourceName, string fileName) {
			Assembly currentAssembly = Assembly.GetExecutingAssembly();
			Stream gifBytes = currentAssembly.GetManifestResourceStream(resourceName);

			// If the file doesn't exist then create it
			if (!File.Exists(fileName)) {
				byte[] fileContent = new byte[gifBytes.Length];
				gifBytes.Read(fileContent, 0, fileContent.Length);
				FileStream newFile = new FileStream(fileName, FileMode.OpenOrCreate);
				newFile.Write(fileContent, 0, fileContent.Length);
				newFile.Close();
			}
		}

		/// <summary>
		/// Serializes the XML document.
		/// </summary>
		/// <param name="providerName">Name of the provider.</param>
		public static void SerializeXMLDocument(ref XmlDataProvider provider) {
			Winform.SaveFileDialog fileDlg;

			if (provider != null &&
				(fileDlg = new Winform.SaveFileDialog()).ShowDialog().Equals(Winform.DialogResult.OK) &&
				fileDlg.FileName.Length > 0)
				provider.Document.Save(fileDlg.FileName);
		}
	}
}