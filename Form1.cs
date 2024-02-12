using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BookforSecrete
{
    public partial class Form1 : Form
    {
        private DiaryEntryForm diaryEntryForm;
        private Dictionary<TabPage, string> tabToFileMapping = new Dictionary<TabPage, string>();
        public Form1()
        {
            InitializeComponent();
            diaryEntryForm = new DiaryEntryForm();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //DiaryEntryForm diaryEntryForm = new DiaryEntryForm();
            diaryEntryForm.Show();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Assuming listViewBooks is the name of your ListView control
            listViewBooks.Items.Clear(); // Clear existing items

            // Iterate through your created books and add their names to the ListView
            foreach (KeyValuePair<TabPage, string> entry in tabToFileMapping)
            {
                string bookName = Path.GetFileNameWithoutExtension(entry.Value);
                ListViewItem item = new ListViewItem(bookName);
                listViewBooks.Items.Add(item);
            }

            // Show the ListView in a new form or panel
            ShowBookListForm();
        }

        private void ShowBookListForm()
        {
            // Create a new form to display the book list
            Form bookListForm = new Form();
            bookListForm.Text = "Book List";
            bookListForm.Icon = new Icon("F://BookforSecrete//BookforSecrete//main.ico");


            // Set up the ListView on the new form
            ListView listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.Columns.Add("Book Name");

            // Add the items from the original ListView
            foreach (ListViewItem item in listViewBooks.Items)
            {
                listView.Items.Add((ListViewItem)item.Clone());
            }

            // Add the ListView to the form
            bookListForm.Controls.Add(listView);

            // Show the new form
            bookListForm.ShowDialog();
        }
        private void openBook_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Book Files|*.book|All Files|*.*";
                openFileDialog.Title = "Open Book File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Create a new instance of DiaryEntryForm
                    DiaryEntryForm diaryEntryForm = new DiaryEntryForm();

                    // Call the OpenBook method in DiaryEntryForm
                   
                    diaryEntryForm.OpenBook(openFileDialog.FileName);
                    diaryEntryForm.Show();
                }
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
           Application.Exit();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
