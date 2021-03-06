using System;
using System.Windows.Forms;


namespace DSShared.Windows
{
	/// <summary>
	/// A generic form providing a textbox for user-input.
	/// </summary>
	public sealed partial class FindFileForm
		:
			Form
	{
		#region Properties
		/// <summary>
		/// Gets the text in the textbox.
		/// </summary>
		public string InputString
		{
			get { return tbInput.Text; }
		}
		#endregion


		#region cTors
		/// <summary>
		/// Main constructor.
		/// </summary>
		/// <param name="notice">text that will be shown above the textbox</param>
		/// <param name="caption">caption for the titlebar</param>
		/// <param name="input">initial value of the textbox</param>
		public FindFileForm(string notice, string caption, string input)
		{
			InitializeComponent();
			
			Text = caption;
			lblNotice.Text = notice;

			tbInput.Text = input;
			tbInput.Select();
		}
		/// <summary>
		/// Auxiliary constructor.
		/// </summary>
		/// <param name="notice"></param>
		public FindFileForm(string notice)
			:
				this(notice, "Input Text", String.Empty)
		{}
		#endregion


		#region Eventcalls
		private void btnFindFile_Click(object sender, EventArgs e)
		{
			using (var f = openFileDialog)	// TODO: Delete the OpenFileDialog class and use stock .NET forms.
			{								// In fact delete this class also and use stock .NET forms.
				f.Title = "Find MCDEdit.exe";
				if (f.ShowDialog() == DialogResult.OK)
					tbInput.Text = f.FileName;
			}
		}
		#endregion
	}
}
