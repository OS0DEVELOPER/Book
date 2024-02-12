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
using Xceed.Words.NET;
using System.Security.Cryptography;

namespace BookforSecrete
{
    public partial class DiaryEntryForm : Form
    {
        private byte[] GenerateRandomKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[32]; // 256 bits for AES-256
                rng.GetBytes(key);
                return key;
            }
        }

        private byte[] GenerateRandomIV()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] iv = new byte[16]; // 128 bits for AES
                rng.GetBytes(iv);
                return iv;
            }
        }

        public DiaryEntryForm()
        {
            InitializeComponent();
        }

        private void DiaryEntryForm_Load(object sender, EventArgs e)
        {

        }
        public void OpenBook(string filePath)
        {
            // Read the key and IV from the file
            byte[] keyFromFile = ReadKeyFromFile(filePath);
            byte[] ivFromFile = ReadIVFromFile(filePath);

            using (StreamReader reader = new StreamReader(filePath))
            {
                tabControl1.TabPages.Clear(); // Clear existing tabs

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // Check if the line represents a page title
                    if (line.StartsWith("Page "))
                    {
                        // Create a new TabPage for each page title
                        TabPage currentPage = new TabPage(line.Substring("Page ".Length));

                        // Read the font settings
                        string fontLine = reader.ReadLine();
                        if (fontLine.StartsWith("Font: "))
                        {
                            string[] fontInfo = fontLine.Substring("Font: ".Length).Split(',');

                            if (fontInfo.Length == 2)
                            {
                                string fontName = fontInfo[0].Trim();
                                float fontSize = float.Parse(fontInfo[1].Trim().Replace("pt", ""));
                                Font font = new Font(fontName, fontSize);

                                // Create a new RichTextBox with the font settings
                                RichTextBox currentRichTextBox = new RichTextBox
                                {
                                    Font = font,
                                    Dock = DockStyle.Fill
                                };

                                // Read the encrypted content
                                string contentLine = reader.ReadLine(); // Read "Content:" line
                                StringBuilder encryptedContentBuilder = new StringBuilder();

                                // Read lines until an empty line is encountered
                                while (!string.IsNullOrWhiteSpace(contentLine))
                                {
                                    encryptedContentBuilder.AppendLine(contentLine);
                                    contentLine = reader.ReadLine();
                                }

                                // Decrypt the content
                                string encryptedContent = encryptedContentBuilder.ToString().Trim();
                                string decryptedContent = Decrypt(encryptedContent, keyFromFile, ivFromFile);

                                // Set the decrypted content to the currentRichTextBox
                                currentRichTextBox.Text = decryptedContent;
                                // Add the RichTextBox to the new TabPage

                                currentRichTextBox.Visible = true;


                                currentRichTextBox.Dock = DockStyle.Fill;
                                
                                
                                currentRichTextBox.Focus();

                                currentPage.Controls.Add(currentRichTextBox);



                            }
                        }

                        // Add the new TabPage to the TabControl
                        tabControl1.TabPages.Add(currentPage);
                    }
                }
            }
        }


        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private Dictionary<TabPage, string> tabToFileMapping = new Dictionary<TabPage, string>();
        private Dictionary<TabPage, Font> tabToFontMapping = new Dictionary<TabPage, Font>();

        private string Encrypt(string plainText, byte[] key, byte[] iv)
        {
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = key;
                rijAlg.IV = iv;

                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }


        private string Decrypt(string cipherText, byte[] key, byte[] iv)
        {
         
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                if(key == null | iv == null | cipherText == null)
                {
                    MessageBox.Show("Unable to Decrypt!");
                    return null;
                }
                rijAlg.Key = key;
                rijAlg.IV = iv;

                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Book Files|*.book|All Files|*.*";
                    saveFileDialog.Title = "Save Book File";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Save the file path associated with the selected tab
                        tabToFileMapping[tabControl1.SelectedTab] = saveFileDialog.FileName;

                        // Save the font associated with the selected tab
                        tabToFontMapping[tabControl1.SelectedTab] = GetCurrentTabRichTextBox().Font;

                        // Save the content and other settings to the file
                        SaveBookToFile(saveFileDialog.FileName);
                        
                        // Save the key and IV to the file
                        byte[] key = GenerateRandomKey();
                        byte[] iv = GenerateRandomIV();
                        SaveKeyAndIVToFile(saveFileDialog.FileName, key, iv);
                        EncryptAndSaveToFile(saveFileDialog.FileName);
                    }
                }
            }
        }
        private void SaveBookToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (TabPage tabPage in tabControl1.TabPages)
                {
                    string pageTitle = tabPage.Text;
                    RichTextBox currentRichTextBox = GetCurrentTabRichTextBox(tabPage);



                    // Write the page title
                    writer.WriteLine($"Page {pageTitle}");

                    // Write the font settings
                    writer.WriteLine($"Font: {currentRichTextBox.Font.Name}, {currentRichTextBox.Font.Size}pt");

                    // Write the content
                    writer.WriteLine("Content:");
                    writer.WriteLine(currentRichTextBox.Text);
                    writer.WriteLine();
                }
            }
        }

        private RichTextBox GetCurrentTabRichTextBox(TabPage tabPage)
        {
            if (tabPage != null)
            {
                RichTextBox richTextBox = tabPage.Controls.OfType<RichTextBox>().FirstOrDefault();
                return richTextBox;
            }
            return null;
        }
        // Helper method to get the RichTextBox of the current tab
        private RichTextBox GetCurrentTabRichTextBox()
        {
            if (tabControl1.SelectedTab != null)
            {
                RichTextBox richTextBox = tabControl1.SelectedTab.Controls.OfType<RichTextBox>().FirstOrDefault();
                return richTextBox;
            }
            return null;
        }

        private void addPage_Click(object sender, EventArgs e)
        {


            // Create a new TabPage
            TabPage newTabPage = new TabPage("New Tab");

            // Create a new RichTextBox
           
            RichTextBox newRichTextBox = new RichTextBox
            {
                Font = new Font("Arial", 20),
                Dock = DockStyle.Fill
            };
            //newRichTextBox.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;

            // Add the RichTextBox to the new TabPage
            newTabPage.Controls.Add(newRichTextBox);
            newRichTextBox.Dock = DockStyle.Fill;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage; // Optionally select the new tab tab
                                                  // Set focus to the RichTextBox within the new tab
            newRichTextBox.Focus();
        }


        private void removePage_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                // Set another tab as selected or none (if desired)
            }
            else
            {
                MessageBox.Show("Please select a tab to remove.");
            }
        }

        private void fontButton_Click(object sender, EventArgs e)
        {
            // Get the currently selected RichTextBox within the selected tab
            RichTextBox selectedRichTextBox = GetSelectedRichTextBox();

            if (selectedRichTextBox != null)
            {
                // Use FontDialog to allow the user to choose the font
                using (FontDialog fontDialog = new FontDialog())
                {
                    if (fontDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Set the selected font to the RichTextBox
                        selectedRichTextBox.Font = fontDialog.Font;
                    }
                }
            }
        }
        // Helper method to get the currently selected RichTextBox
        private RichTextBox GetSelectedRichTextBox()
        {
            if (tabControl1.SelectedTab != null && tabControl1.SelectedTab.Controls.Count > 0)
            {
                Control control = tabControl1.SelectedTab.Controls[0];

                if (control is RichTextBox richTextBox)
                {
                    return richTextBox;
                }
            }

            return null;
        }
        // Helper method to save text to a file
        private void SaveTextToFile(string text, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(text);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Book Files|*.book|All Files|*.*";
                openFileDialog.Title = "Open Book File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Your existing code to load and populate tabs
                    LoadBookFromFile(openFileDialog.FileName);

                    // Decrypt and load from file only if a file was selected
                    DecryptAndLoadFromFile(openFileDialog.FileName);
                }
                // If DialogResult is not OK, do nothing or handle it as needed
            }
        }

        private void LoadBookFromFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                if (tabControl1.IsDisposed)
                {
                    return;
                }
                tabControl1.TabPages.Clear(); // Clear existing tabs
                                              // Read the key and IV from the file
                byte[] keyFromFile = ReadKeyFromFile(filePath);
                byte[] ivFromFile = ReadIVFromFile(filePath);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // Check if the line represents a page title
                    if (line.StartsWith("Page "))
                    {
                        // Create a new TabPage for each page title
                        TabPage currentPage = new TabPage(line.Substring("Page ".Length));

                        // Read the font settings
                        string fontLine = reader.ReadLine();
                        if (fontLine.StartsWith("Font: "))
                        {
                            string[] fontInfo = fontLine.Substring("Font: ".Length).Split(',');

                            if (fontInfo.Length == 2)
                            {
                                string fontName = fontInfo[0].Trim();
                                float fontSize = float.Parse(fontInfo[1].Trim().Replace("pt", ""));
                                Font font = new Font(fontName, fontSize);

                                // Create a new RichTextBox with the font settings
                                RichTextBox currentRichTextBox = new RichTextBox
                                {
                                    Font = font,
                                    Dock = DockStyle.Fill
                                };

                                // Add the RichTextBox to the new TabPage
                                currentPage.Controls.Add(currentRichTextBox);

                                // Read the content
                                string contentLine;
                                StringBuilder contentBuilder = new StringBuilder();
                                while (!string.IsNullOrWhiteSpace(contentLine = reader.ReadLine()))
                                {
                                    contentBuilder.AppendLine(contentLine);
                                }

                                // Set the content to the currentRichTextBox
                                currentRichTextBox.Text = contentBuilder.ToString().Trim();
                            }
                        }

                        // Add the new TabPage to the TabControl
                        tabControl1.TabPages.Add(currentPage);
                    }
                }
            }
        }
        private void SaveKeyAndIVToFile(string filePath, byte[] key, byte[] iv)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                // Save the key and IV at the end of the file
                writer.WriteLine($"Key: {Convert.ToBase64String(key)}");
                writer.WriteLine($"IV: {Convert.ToBase64String(iv)}");
            }
        }

        private byte[] ReadKeyFromFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                // Read the key from the file
                string keyLine = reader.ReadToEnd().Split('\n')
                    .LastOrDefault(line => line != null && line.StartsWith("Key: "));
                return keyLine != null ? Convert.FromBase64String(keyLine.Substring("Key: ".Length)) : null;
            }
        }

        private byte[] ReadIVFromFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                // Read the IV from the file
                string ivLine = reader.ReadToEnd().Split('\n')
                    .LastOrDefault(line => line != null && line.StartsWith("IV: "));
                return ivLine != null ? Convert.FromBase64String(ivLine.Substring("IV: ".Length)) : null;
            }
        }
        private void EncryptAndSaveToFile(string filePath)
        {
            byte[] key = GenerateRandomKey();
            byte[] iv = GenerateRandomIV();

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Save the key and IV at the beginning of the file
                writer.WriteLine($"Key: {Convert.ToBase64String(key)}");
                writer.WriteLine($"IV: {Convert.ToBase64String(iv)}");

                foreach (TabPage tabPage in tabControl1.TabPages)
                {
                    string pageTitle = tabPage.Text;
                    RichTextBox currentRichTextBox = GetCurrentTabRichTextBox(tabPage);

                    // Write the page title and font settings
                    writer.WriteLine($"Page: {pageTitle}");
                    writer.WriteLine($"Font: {currentRichTextBox.Font.Name}, {currentRichTextBox.Font.Size}pt");

                    // Write the encrypted content
                    writer.WriteLine("Content:");
                    string encryptedContent = Encrypt(currentRichTextBox.Text, key, iv);
                    writer.WriteLine(encryptedContent);
                    writer.WriteLine();
                }
            }
        }

        private void DecryptAndLoadFromFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                // Read the key and IV from the file
                string keyLine = reader.ReadLine();
                string ivLine = reader.ReadLine();

                if (keyLine == null || !keyLine.StartsWith("Key: ") ||
                    ivLine == null || !ivLine.StartsWith("IV: "))
                {
                    MessageBox.Show("Invalid file format. Cannot decrypt.");
                    return;
                }

                byte[] key = Convert.FromBase64String(keyLine.Substring("Key: ".Length));
                byte[] iv = Convert.FromBase64String(ivLine.Substring("IV: ".Length));

                tabControl1.TabPages.Clear(); // Clear existing tabs

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // Check if the line represents a page title
                    if (line.StartsWith("Page: "))
                    {
                        // Create a new TabPage for each page title
                        TabPage currentPage = new TabPage(line.Substring("Page: ".Length));

                        // Read the font settings
                        string fontLine = reader.ReadLine();
                        if (fontLine.StartsWith("Font: "))
                        {
                            string[] fontInfo = fontLine.Substring("Font: ".Length).Split(',');

                            if (fontInfo.Length == 2)
                            {
                                string fontName = fontInfo[0].Trim();
                                float fontSize = float.Parse(fontInfo[1].Trim().Replace("pt", ""));
                                Font font = new Font(fontName, fontSize);

                                // Create a new RichTextBox with the font settings
                                RichTextBox currentRichTextBox = new RichTextBox
                                {
                                    Font = font,
                                    Dock = DockStyle.Fill
                                };

                                // Read the encrypted content
                                reader.ReadLine(); // Skip "Content:" line
                                string encryptedContent = reader.ReadLine();
                                string decryptedContent = Decrypt(encryptedContent, key, iv);

                                // Set the decrypted content to the currentRichTextBox
                                currentRichTextBox.Text = decryptedContent;

                                // Add the RichTextBox to the new TabPage
                                currentPage.Controls.Add(currentRichTextBox);
                            }
                        }

                        // Add the new TabPage to the TabControl
                        tabControl1.TabPages.Add(currentPage);
                    }
                }
            }
        }
        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
