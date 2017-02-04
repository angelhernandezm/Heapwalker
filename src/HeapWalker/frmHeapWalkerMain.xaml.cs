/* File: frmHeapWalkerMain.cs
   Framework version:  .NET Framework version 3.5
   C# compiler: Microsoft (R) Visual C# 2008 Compiler version 3.5.21022.8
   Creation date: July 21st, 2008
   Developer: Angel J. Hernández
   e-m@ail: angeljesus14@hotmail.com
   Blog: http://msmvps.com/blogs/angelhernandez
   Description: Main HeapWalker window
*/

using System;
using System.IO;
using System.IO.IsolatedStorage;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using Winform = System.Windows.Forms;
using System.Xml;
using System.Security;
using System.Security.Principal;
using Win32Support;

namespace HeapWalker {
	/// <summary>
	/// Interaction logic for frmHeapWalkerMain.xaml
	/// </summary>
	public partial class frmHeapWalkerMain : Window {
		#region "Delegates"
		private delegate void GenericCallback<T>(T type);
		#endregion

		#region "Consts"
		private const string ANIMATEDGIFNAME = "HeapWalker.Resources.Images.Processing.gif";
		#endregion

		#region "Members"
		private string _animatedgifpath = Environment.GetEnvironmentVariable("TEMP") + "\\processing.gif";
		#endregion

		#region "Properties"
		private string AnimatedGifPath {
			get {
				return _animatedgifpath;
			}
		}
		#endregion

		#region "Ctors"
		public frmHeapWalkerMain() {
			InitializeComponent();
		}
		#endregion

		#region "Event Handlers"
		/// <summary>
		/// Handles the Click event of the btnRefreshProcessList control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void btnRefreshProcessList_Click(object sender, RoutedEventArgs e) {
			EnumerateProcesses();
		}

		/// <summary>
		/// Handles the SelectionChanged event of the cboProcessName control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
		private void cboProcessName_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			KeyValuePair<int, string> selected = (KeyValuePair<int, string>)cboProcessName.SelectedItem;
			BackgroundWorker heapAsyncThread = new BackgroundWorker();
			heapAsyncThread.DoWork += new DoWorkEventHandler(heapAsyncThread_DoWork);
			heapAsyncThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(heapAsyncThread_RunWorkerCompleted);
			heapAsyncThread.RunWorkerAsync(selected);
		}

		/// <summary>
		/// Handles the Loaded event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			// Extract Animated Gif from Assembly
			ExtractAnimatedGifFromAssembly();
			// Enumerate and load Processes' Dropdown list
			EnumerateProcesses();
		}

		/// <summary>
		/// Handles the SelectionChanged event of the lstHeapNodes control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
		private void lstHeapNodes_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			XmlElement heapNodes;
			XmlDataProvider xmlBlocks = TryFindResource("xmlBlockData") as XmlDataProvider;
			int selectedIndex = lstHeapNodes.SelectedIndex > -1 ? lstHeapNodes.SelectedIndex : 0;

			if (lstHeapNodes.Items.Count > 0 && xmlBlocks != null &&
				(heapNodes = (XmlElement)lstHeapNodes.Items[selectedIndex]) != null) {
				selectedIndex = int.Parse(heapNodes.Attributes["index"].Value);
				xmlBlocks.XPath = "/HeapWalker/Process/Heap[@index='" + selectedIndex.ToString() + "']/*";
			} else
				xmlBlocks.XPath = string.Empty;
		}

		/// <summary>
		/// Handles the Click event of the btnSaveHeaps control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void btnSaveHeaps_Click(object sender, RoutedEventArgs e) {
			XmlDataProvider xmlHeaps = TryFindResource("xmlHeapData") as XmlDataProvider;
			Common.SerializeXMLDocument(ref xmlHeaps);
		}

		/// <summary>
		/// Handles the MouseDoubleClick event of the lstHeapBlocks control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
		private void lstHeapBlocks_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (lstHeapBlocks.SelectedItem != null)
				(new frmMemoryDump((XmlElement)lstHeapBlocks.SelectedItem)).ShowDialog();
		}

		/// <summary>
		/// Handles the LeftClick event of the lblAbout control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
		private void lblAbout_LeftClick(object sender, MouseButtonEventArgs e) {
			(new frmAbout()).ShowDialog();
		}
		#endregion

		#region "Support Methods"
		/// <summary>
		/// 
		/// </summary>
		private void EnumerateProcesses() {
			cboProcessName.ItemsSource = Utilities.EnumRunningProcesses();
		}

		/// <summary>
		/// Manages the processing animation.
		/// </summary>
		/// <param name="args">The args.</param>
		private void ManageProcessingAnimation(object args) {
			MediaElement selectedImage = (((object[])args)[0]) as MediaElement;
			bool state = (bool)(((object[])args)[1]);

			selectedImage.Source = new Uri(AnimatedGifPath);
			selectedImage.Visibility = state ? Visibility.Visible : Visibility.Hidden;

			selectedImage.MediaEnded += delegate(object sender, RoutedEventArgs e) {
				selectedImage.Source = null;
				selectedImage.Source = new Uri(AnimatedGifPath);
			};
		}

		/// <summary>
		/// Extracts the animated GIF from assembly.
		/// </summary>
		private void ExtractAnimatedGifFromAssembly() {
			Common.ExtractImageFromAssembly(ANIMATEDGIFNAME, AnimatedGifPath);
		}
		#endregion

		#region "Async Support Methods"
		/// <summary>
		/// Handles the RunWorkerCompleted event of the heapAsyncThread control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
		private void heapAsyncThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			KeyValuePair<int, string> selected = (KeyValuePair<int, string>)cboProcessName.SelectedItem;
			XmlDataProvider xmlHeaps = TryFindResource("xmlHeapData") as XmlDataProvider;
			XmlDataProvider xmlBlocks = TryFindResource("xmlBlockData") as XmlDataProvider;

			if (e.Error == null && xmlHeaps != null && xmlBlocks != null)
				xmlHeaps.Document = xmlBlocks.Document = (XmlDocument)e.Result;

			GenericCallback<object> handleImage = new GenericCallback<object>(ManageProcessingAnimation);

			imgHeapLoad.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
																		handleImage, new object[] { imgHeapLoad, false });
		}

		/// <summary>
		/// Handles the DoWork event of the heapAsyncThread control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
		private void heapAsyncThread_DoWork(object sender, DoWorkEventArgs e) {
			GenericCallback<object> handleImage = new GenericCallback<object>(ManageProcessingAnimation);

			imgHeapLoad.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
																		handleImage, new object[] { imgHeapLoad, true });

			e.Result = HeapUtil.GetHeapBasedOnProcessID(((KeyValuePair<int, string>)e.Argument).Key, 0, null);

		}
		#endregion
	}
}