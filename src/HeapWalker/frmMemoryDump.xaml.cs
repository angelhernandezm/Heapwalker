/* File: frmMemoryDump.cs
   Framework version:  .NET Framework version 3.5
   C# compiler: Microsoft (R) Visual C# 2008 Compiler version 3.5.21022.8
   Creation date: July 21st, 2008
   Developer: Angel J. Hernández
   e-m@ail: angeljesus14@hotmail.com
   Blog: http://msmvps.com/blogs/angelhernandez
   Description: Memory dump window
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
using Winform = System.Windows.Forms;
using Win32Support;

namespace HeapWalker {
	/// <summary>
	/// Interaction logic for frmMemoryDump.xaml
	/// </summary>
	public partial class frmMemoryDump : Window {
		#region "Structs"
		private struct MemoryBlock {
			#region "Properties"
			public int ProcessId {
				get;
				set;
			}

			public int BlockSize {
				get;
				set;
			}

			public int BlockAddress {
				get;
				set;
			}
			#endregion

			#region "Ctor"
			public MemoryBlock(XmlElement selectedNode)
				: this() {
				ProcessId = int.Parse(selectedNode.ParentNode.ParentNode.Attributes["id"].Value);
				BlockSize = int.Parse(selectedNode.Attributes["blockSizeAsDecimal"].Value);
				BlockAddress = int.Parse(selectedNode.Attributes["blockAddressAsDecimal"].Value);
			}
			#endregion

		}

		#endregion

		#region "Members"
		private XmlElement _selectednode;
		#endregion

		#region "Properties"
		public XmlElement SelectedNode {
			get {
				return _selectednode;
			}
			set {
				_selectednode = value;
				if (value != null)
					RetrieveMemoryBlock();
			}
		}
		#endregion

		#region "Ctors"
		public frmMemoryDump() {
			InitializeComponent();
		}

		public frmMemoryDump(XmlElement selectedNode)
			: this() {
			SelectedNode = selectedNode;
		}
		#endregion

		#region "Methods"
		/// <summary>
		/// Retrieves the memory block.
		/// </summary>
		protected virtual void RetrieveMemoryBlock() {
			BackgroundWorker heapBlockAsyncThread = new BackgroundWorker();
			heapBlockAsyncThread.DoWork += new DoWorkEventHandler(heapBlockAsyncThread_DoWork);
			heapBlockAsyncThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(heapBlockAsyncThread_RunWorkerCompleted);
			heapBlockAsyncThread.RunWorkerAsync(new MemoryBlock(_selectednode));
		}

		#endregion

		#region "Async Support Methods"
		/// <summary>
		/// Handles the RunWorkerCompleted event of the heapAsyncThread control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
		private void heapBlockAsyncThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			XmlDataProvider xmlMemoryDump = TryFindResource("xmlHeapBlock") as XmlDataProvider;

			// Successful retrieved memory block?
			if (e.Result != null && xmlMemoryDump != null) {
				xmlMemoryDump.Document = (XmlDocument)e.Result;
				xmlMemoryDump.XPath = "/HeapWalker/Block/*";
			}
		}

		/// <summary>
		/// Handles the DoWork event of the heapAsyncThread control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
		private void heapBlockAsyncThread_DoWork(object sender, DoWorkEventArgs e) {
			StringBuilder memoryBlock = new StringBuilder();
			MemoryBlock selectedBlock = (MemoryBlock)e.Argument;
			e.Result = HeapUtil.GetHeapBasedOnProcessID(selectedBlock.ProcessId, selectedBlock.BlockAddress, memoryBlock);
		}
		#endregion

		#region "Event Handlers"
		/// <summary>
		/// Handles the Loaded event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			lblSelectedNodeInfo.Content = string.Format(lblSelectedNodeInfo.Content.ToString(),
														new object[] {_selectednode.Attributes["blockAddress"].Value,
															          _selectednode.Attributes["blockSize"].Value});
		}

		/// <summary>
		/// Handles the KeyDown event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
		private void Window_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		/// <summary>
		/// Handles the Click event of the btnSaveBlock control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void btnSaveBlock_Click(object sender, RoutedEventArgs e) {
			XmlDataProvider xmlHeaps = TryFindResource("xmlHeapBlock") as XmlDataProvider;
			Common.SerializeXMLDocument(ref xmlHeaps);
		}
		#endregion
	}
}