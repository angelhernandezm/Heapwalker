/* File: frmAbout.cs
   Framework version:  .NET Framework version 3.5
   C# compiler: Microsoft (R) Visual C# 2008 Compiler version 3.5.21022.8
   Creation date: July 21st, 2008
   Developer: Angel J. Hernández
   e-m@ail: angeljesus14@hotmail.com
   Blog: http://msmvps.com/blogs/angelhernandez
   Description: HeapWalker about form
*/

using System;
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
using System.Threading;
using System.Diagnostics;

namespace HeapWalker {
	/// <summary>
	/// Interaction logic for frmAbout.xaml
	/// </summary>
	public partial class frmAbout : Window {
		#region "Consts"
		private const string MVPPNGNAME = "HeapWalker.Resources.Images.MVP_Logo.png";
		#endregion

		#region "Members"
		private string _mvppngpath = Environment.GetEnvironmentVariable("TEMP") + "\\mvp_logo.png";
		#endregion

		#region "Properties"
		private string MVPPngPath {
			get {
				return _mvppngpath;
			}
		}
		#endregion

		#region "Ctor"
		public frmAbout() {
			InitializeComponent();
		}
		#endregion

		#region "Support Methods"
		/// <summary>
		/// Extracts the MVP PNG from assembly.
		/// </summary>
		private void ExtractMVPPngFromAssembly() {
			Common.ExtractImageFromAssembly(MVPPNGNAME, MVPPngPath);
			imgMVPLogo.Source = new BitmapImage(new Uri(MVPPngPath));
		}
		#endregion

		#region "Support Methods"
		/// <summary>
		/// Starts the browser.
		/// </summary>
		private void StartBrowser() {
			Process.Start("http://msmvps.com/blogs/angelhernandez");
		}
		#endregion

		#region "Event Handlers"
		/// <summary>
		/// Handles the Loaded event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			// Extract MVP Png from Assembly
			ExtractMVPPngFromAssembly();
		}

		/// <summary>
		/// Handles the KeyUp event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
		private void Window_KeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		/// <summary>
		/// Handles the MouseLeftButtonUp event of the lblAbout control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
		private void lblAbout_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			(new Thread(new ThreadStart(StartBrowser))).Start();
		}
		#endregion
	}
}