using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ClothingStoreApp
{
    public class AddProductForm : Form
    {
        [DllImport("user32.dll")]
        private static extern void ShowTouchKeyboard();

        [DllImport("user32.dll")]
        private static extern void HideTouchKeyboard();

        private readonly string connectionString = "Host=localhost;Port=5432;Database=clothing_store;Username=postgres;Password=0000;";

        private TextBox nameBox, categoryBox, barcodeBox;
        private NumericUpDown priceBox, quantityBox, tvaRateBox;
        private Button saveBtn, cancelBtn, generateBarcodeBtn, keyboardBtn;
        private CheckBox useSizesColorsCheckBox, applyTvaCheckBox;
        private FlowLayoutPanel sizesFlowPanel, colorsFlowPanel;
        private Label barcodeLabel, priceHtLabel, tvaAmountLabel, priceTtcLabel;
        private Panel mainPanel;

        private Panel tvaDisplayPanelRef;
        private NumericUpDown tvaRateBoxRef;
        private Panel sizesColorsContentPanelRef;

        // عناصر التحكم في TVA
        private Label tauxTvaLabelRef;
        private Label percentLabelRef;

        public AddProductForm()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            // Configuration de base du formulaire
            this.Text = "➕ NOUVEAU PRODUIT - CAISSE";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 245, 249);

            // Panel principal avec scroll
            mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(30);
            mainPanel.AutoScroll = true;
            this.Controls.Add(mainPanel);

            int yPos = 20;

            // ========== TITRE PRINCIPAL ==========
            Label titleLabel = new Label
            {
                Text = "➕ AJOUTER UN NOUVEAU PRODUIT",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            mainPanel.Controls.Add(titleLabel);
            yPos += 70;

            // ========== LIGNE 1: NOM ET CATÉGORIE ==========
            Panel firstRow = CreateRowPanel(yPos);

            // Nom du produit
            firstRow.Controls.Add(CreateLabel("NOM DU PRODUIT *", 0, 10, 250));
            nameBox = CreateTextBox();
            nameBox.Location = new Point(260, 10);
            nameBox.Size = new Size(400, 50);
            firstRow.Controls.Add(nameBox);

            // Catégorie
            firstRow.Controls.Add(CreateLabel("CATÉGORIE", 680, 10, 250));
            categoryBox = CreateTextBox();
            categoryBox.Location = new Point(940, 10);
            categoryBox.Size = new Size(350, 50);
            categoryBox.PlaceholderText = "Optionnel";
            firstRow.Controls.Add(categoryBox);

            mainPanel.Controls.Add(firstRow);
            yPos += 90;

            // ========== LIGNE 2: CODE BARRES ET QUANTITÉ ==========
            Panel secondRow = CreateRowPanel(yPos);

            // Code barres
            secondRow.Controls.Add(CreateLabel("CODE BARRES", 0, 10, 250));

            Panel barcodePanel = new Panel
            {
                Location = new Point(260, 10),
                Size = new Size(400, 50),
                BackColor = Color.Transparent
            };

            barcodeBox = CreateTextBox();
            barcodeBox.Size = new Size(250, 50);
            barcodeBox.PlaceholderText = "Généré automatiquement";

            generateBarcodeBtn = new CustomButton("🔄 GÉNÉRER", 260, 0, 130, 50);
            generateBarcodeBtn.Click += (s, e) => GenererEtAfficherCodeBarres();
            barcodePanel.Controls.Add(barcodeBox);
            barcodePanel.Controls.Add(generateBarcodeBtn);
            secondRow.Controls.Add(barcodePanel);

            // Quantité
            secondRow.Controls.Add(CreateLabel("QUANTITÉ *", 680, 10, 250));
            quantityBox = CreateNumericBox();
            quantityBox.Location = new Point(940, 10);
            quantityBox.Size = new Size(350, 50);
            quantityBox.DecimalPlaces = 0;
            secondRow.Controls.Add(quantityBox);

            mainPanel.Controls.Add(secondRow);
            yPos += 90;

            // ========== LIGNE 3: PRIX ET TVA ==========
            Panel thirdRow = CreateRowPanel(yPos);

            // Prix de vente
            thirdRow.Controls.Add(CreateLabel("PRIX DE VENTE :", 0, 10, 250));
            priceBox = CreateNumericBox();
            priceBox.Location = new Point(260, 10);
            priceBox.Size = new Size(400, 50);
            priceBox.ValueChanged += (s, e) => CalculerTVA();
            thirdRow.Controls.Add(priceBox);

            // TVA Panel - منظم بشكل أفضل
            Panel tvaPanel = new Panel
            {
                Location = new Point(680, 10),
                Size = new Size(600, 50),
                BackColor = Color.Transparent
            };

            applyTvaCheckBox = new CheckBox
            {
                Text = "APPLIQUER LA TVA",
                Location = new Point(0, 10),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.Transparent
            };
            applyTvaCheckBox.CheckedChanged += (s, e) => BasculerTVA();
            tvaPanel.Controls.Add(applyTvaCheckBox);

            // Label pour le taux TVA (مخفي في البداية)
            Label tauxTvaLabel = new Label
            {
                Text = "Taux:",
                Location = new Point(200, 15),
                Size = new Size(40, 20),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Visible = false
            };
            tvaPanel.Controls.Add(tauxTvaLabel);
            this.tauxTvaLabelRef = tauxTvaLabel;

            tvaRateBox = CreateNumericBox();
            tvaRateBox.Location = new Point(300, 0);
            tvaRateBox.Size = new Size(90, 60);
            tvaRateBox.Value = 20.00m;
            tvaRateBox.ValueChanged += (s, e) => CalculerTVA();
            tvaRateBox.Font = new Font("Segoe UI", 12);
            tvaRateBox.Visible = false;
            tvaPanel.Controls.Add(tvaRateBox);
            this.tvaRateBoxRef = tvaRateBox;

            // Label pour le pourcentage (مخفي في البداية)
            Label percentLabel = new Label
            {
                Text = "%",
                Location = new Point(300, 15),
                Size = new Size(20, 20),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Visible = false
            };
            tvaPanel.Controls.Add(percentLabel);
            this.percentLabelRef = percentLabel;

            thirdRow.Controls.Add(tvaPanel);
            mainPanel.Controls.Add(thirdRow);
            yPos += 90;

            // ========== AFFICHAGE TVA (مخفي في البداية) ==========
            Panel tvaDisplayPanel = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(1300, 80),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            priceHtLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(250, 50),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Text = "PRIX : 0.00 €",
                TextAlign = ContentAlignment.MiddleLeft
            };

            tvaAmountLabel = new Label
            {
                Location = new Point(300, 20),
                Size = new Size(200, 50),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(239, 68, 68),
                Text = "TVA: 0.00 €",
                TextAlign = ContentAlignment.MiddleLeft
            };

            // TOTAL TTC
            priceTtcLabel = new Label
            {
                Location = new Point(550, 10),
                Size = new Size(400, 50),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138),
                Text = "TOTAL TTC: 0.00 €",
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            tvaDisplayPanel.Controls.Add(priceHtLabel);
            tvaDisplayPanel.Controls.Add(tvaAmountLabel);
            tvaDisplayPanel.Controls.Add(priceTtcLabel);
            mainPanel.Controls.Add(tvaDisplayPanel);

            this.tvaDisplayPanelRef = tvaDisplayPanel;
            yPos += 90;

            // ========== SECTION TAILLES ET COULEURS ==========
            Panel sizesColorsPanel = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(1300, 180),
                BackColor = Color.Transparent
            };

            useSizesColorsCheckBox = new CheckBox
            {
                Text = "🎯 GÉRER LES TAILLES ET COULEURS",
                Location = new Point(0, 0),
                Size = new Size(350, 40),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.Transparent
            };
            useSizesColorsCheckBox.CheckedChanged += (s, e) => BasculerTaillesCouleurs();
            sizesColorsPanel.Controls.Add(useSizesColorsCheckBox);

            // Panel pour tailles et couleurs
            Panel sizesColorsContentPanel = new Panel
            {
                Location = new Point(0, 50),
                Size = new Size(1300, 120),
                Visible = false,
                BackColor = Color.Transparent
            };

            // Tailles
            GroupBox sizesGroup = new GroupBox
            {
                Text = "  TAILLES DISPONIBLES  ",
                Location = new Point(0, 0),
                Size = new Size(600, 120),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White
            };

            sizesFlowPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 25),
                Size = new Size(560, 85),
                BackColor = Color.White,
                AutoScroll = true
            };
            CreerBoutonsTailles();
            sizesGroup.Controls.Add(sizesFlowPanel);
            sizesColorsContentPanel.Controls.Add(sizesGroup);

            // Couleurs
            GroupBox colorsGroup = new GroupBox
            {
                Text = "  COULEURS DISPONIBLES  ",
                Location = new Point(620, 0),
                Size = new Size(600, 120),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White
            };

            colorsFlowPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 25),
                Size = new Size(560, 85),
                BackColor = Color.White,
                AutoScroll = true
            };
            CreerBoutonsCouleurs();
            colorsGroup.Controls.Add(colorsFlowPanel);
            sizesColorsContentPanel.Controls.Add(colorsGroup);

            sizesColorsPanel.Controls.Add(sizesColorsContentPanel);
            mainPanel.Controls.Add(sizesColorsPanel);

            this.sizesColorsContentPanelRef = sizesColorsContentPanel;
            yPos += 200;

            // ========== MESSAGE CODE BARRES ==========
            barcodeLabel = new Label
            {
                Location = new Point(30, yPos),
                Size = new Size(600, 30),
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = Color.FromArgb(107, 114, 128),
                Text = "Le code-barres sera généré automatiquement si le champ est vide"
            };
            mainPanel.Controls.Add(barcodeLabel);
            yPos += 50;

            // ========== BOUTONS D'ACTION ==========
            Panel buttonsPanel = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(1300, 80),
                BackColor = Color.Transparent
            };

            // Bouton clavier
            keyboardBtn = new CustomButton("⌨️ CLAVIER TACTILE", 0, 0, 250, 70);
            keyboardBtn.Click += (s, e) => MontrerClavierTactileManuel();

            // Bouton sauvegarde
            saveBtn = new CustomButton("💾 SAUVEGARDER", 270, 0, 350, 70, Color.FromArgb(16, 185, 129));
            saveBtn.Click += (s, e) => SauvegarderProduit();

            // Bouton annulation
            cancelBtn = new CustomButton("❌ ANNULER", 640, 0, 350, 70, Color.FromArgb(239, 68, 68));
            cancelBtn.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonsPanel.Controls.Add(keyboardBtn);
            buttonsPanel.Controls.Add(saveBtn);
            buttonsPanel.Controls.Add(cancelBtn);
            mainPanel.Controls.Add(buttonsPanel);

            // Activation du clavier tactile
            ActiverClavierTactilePourTousLesChamps();
        }

        private Panel CreateRowPanel(int yPos)
        {
            return new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(1300, 70),
                BackColor = Color.Transparent
            };
        }

        private Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private TextBox CreateTextBox()
        {
            return new TextBox
            {
                Font = new Font("Segoe UI", 14),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Color.FromArgb(55, 65, 81),
                Height = 50
            };
        }

        private NumericUpDown CreateNumericBox()
        {
            return new NumericUpDown
            {
                Font = new Font("Segoe UI", 14),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Color.FromArgb(55, 65, 81),
                Minimum = 0,
                Maximum = 99999,
                DecimalPlaces = 2,
                Height = 50
            };
        }

        private void CreerBoutonsTailles()
        {
            string[] tailles = { "XS", "S", "M", "L", "XL", "XXL", "36", "38", "40", "42", "44", "46", "48" };

            foreach (string taille in tailles)
            {
                CheckBox checkBox = new CheckBox
                {
                    Text = taille,
                    Size = new Size(70, 40),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(243, 244, 246),
                    ForeColor = Color.FromArgb(55, 65, 81)
                };
                checkBox.FlatAppearance.BorderSize = 1;
                checkBox.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
                checkBox.FlatAppearance.CheckedBackColor = Color.FromArgb(59, 130, 246);
                checkBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 231, 235);

                sizesFlowPanel.Controls.Add(checkBox);
            }
        }

        private void CreerBoutonsCouleurs()
        {
            var couleurs = new[]
            {
                new { Nom = "NOIR", Couleur = Color.Black },
                new { Nom = "BLANC", Couleur = Color.White },
                new { Nom = "ROUGE", Couleur = Color.Red },
                new { Nom = "BLEU", Couleur = Color.Blue },
                new { Nom = "VERT", Couleur = Color.Green },
                new { Nom = "JAUNE", Couleur = Color.Yellow },
                new { Nom = "ROSE", Couleur = Color.Pink },
                new { Nom = "VIOLET", Couleur = Color.Purple },
                new { Nom = "ORANGE", Couleur = Color.Orange }
            };

            foreach (var couleur in couleurs)
            {
                CheckBox checkBox = new CheckBox
                {
                    Text = couleur.Nom,
                    Size = new Size(90, 40),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = couleur.Couleur,
                    ForeColor = couleur.Couleur.GetBrightness() > 0.5 ? Color.Black : Color.White
                };
                checkBox.FlatAppearance.BorderSize = 1;
                checkBox.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
                checkBox.FlatAppearance.CheckedBackColor = Color.FromArgb(
                    Math.Min(couleur.Couleur.R + 30, 255),
                    Math.Min(couleur.Couleur.G + 30, 255),
                    Math.Min(couleur.Couleur.B + 30, 255)
                );

                colorsFlowPanel.Controls.Add(checkBox);
            }
        }

        private void GenererEtAfficherCodeBarres()
        {
            string codeBarres = GenererCodeBarres();
            barcodeBox.Text = codeBarres;
            barcodeLabel.Text = $"✅ Code-barres généré: {codeBarres}";
            barcodeLabel.ForeColor = Color.FromArgb(16, 185, 129);
        }

        private void BasculerTVA()
        {
            bool tvaActive = applyTvaCheckBox.Checked;

            // إظهار/إخفاء خيارات TVA
            tvaRateBoxRef.Visible = tvaActive;
            tauxTvaLabelRef.Visible = tvaActive;
            percentLabelRef.Visible = tvaActive;
            tvaDisplayPanelRef.Visible = tvaActive;

            CalculerTVA();
        }

        private void BasculerTaillesCouleurs()
        {
            sizesColorsContentPanelRef.Visible = useSizesColorsCheckBox.Checked;
        }

        private void CalculerTVA()
        {
            decimal prixHT = priceBox.Value;

            if (prixHT > 0)
            {
                if (applyTvaCheckBox.Checked)
                {
                    // حساب TVA من السعر HT
                    decimal tauxTVA = tvaRateBoxRef.Value / 100;
                    decimal montantTVA = prixHT * tauxTVA;
                    decimal prixTTC = prixHT + montantTVA;

                    int tauxTVAEntier = (int)tvaRateBoxRef.Value;

                    priceHtLabel.Text = $"PRIX : {prixHT:N2} €";
                    tvaAmountLabel.Text = $"TVA ({tauxTVAEntier}%): {montantTVA:N2} €";
                    priceTtcLabel.Text = $"TOTAL TTC: {prixTTC:N2} €";
                    Console.WriteLine($"TVA calculée: {montantTVA}");
                }
                else
                {
                    // بدون TVA، السعر HT هو نفسه TTC
                    priceHtLabel.Text = $"PRIX : {prixHT:N2} €";
                    tvaAmountLabel.Text = "TVA: 0.00 €";
                    priceTtcLabel.Text = $"TOTAL TTC: {prixHT:N2} €";
                }
            }
            else
            {
                // عندما يكون السعر 0
                priceHtLabel.Text = "PRIX : 0.00 €";
                tvaAmountLabel.Text = "TVA: 0.00 €";
                priceTtcLabel.Text = "TOTAL TTC: 0.00 €";
            }
        }

        private void ActiverClavierTactilePourTousLesChamps()
        {
            MontrerClavierTactilePourTextBox(nameBox);
            MontrerClavierTactilePourTextBox(categoryBox);
            MontrerClavierTactilePourTextBox(barcodeBox);
            ActiverTactilePourNumericBox(priceBox);
            ActiverTactilePourNumericBox(quantityBox);
            ActiverTactilePourNumericBox(tvaRateBoxRef);
        }

        private void MontrerClavierTactilePourTextBox(TextBox textBox)
        {
            textBox.GotFocus += (s, e) => { try { ShowTouchKeyboard(); } catch { } };
            textBox.LostFocus += (s, e) => { try { HideTouchKeyboard(); } catch { } };
        }

        private void ActiverTactilePourNumericBox(NumericUpDown numericBox)
        {
            numericBox.GotFocus += (s, e) => { try { ShowTouchKeyboard(); } catch { } };
            numericBox.LostFocus += (s, e) => { try { HideTouchKeyboard(); } catch { } };
        }

        private void MontrerClavierTactileManuel()
        {
            try { ShowTouchKeyboard(); }
            catch
            {
                try { System.Diagnostics.Process.Start("tabtip.exe"); }
                catch
                {
                    try { System.Diagnostics.Process.Start("osk.exe"); }
                    catch
                    {
                        MessageBox.Show("Ouvrez le clavier tactile manuellement", "Clavier",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void SauvegarderProduit()
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                MessageBox.Show("Veuillez saisir le nom du produit.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameBox.Focus();
                return;
            }

            if (priceBox.Value <= 0)
            {
                MessageBox.Show("Veuillez saisir un prix de vente valide.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                priceBox.Focus();
                return;
            }

            try
            {
                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                string codeBarres = string.IsNullOrWhiteSpace(barcodeBox.Text) ? GenererCodeBarres() : barcodeBox.Text.Trim();

                // Collecter les tailles et couleurs sélectionnées
                string taillesSelectionnees = "";
                string couleursSelectionnees = "";

                if (useSizesColorsCheckBox.Checked)
                {
                    var listeTailles = new List<string>();
                    foreach (Control control in sizesFlowPanel.Controls)
                    {
                        if (control is CheckBox checkBox && checkBox.Checked)
                            listeTailles.Add(checkBox.Text);
                    }
                    taillesSelectionnees = string.Join(",", listeTailles);

                    var listeCouleurs = new List<string>();
                    foreach (Control control in colorsFlowPanel.Controls)
                    {
                        if (control is CheckBox checkBox && checkBox.Checked)
                            listeCouleurs.Add(checkBox.Text);
                    }
                    couleursSelectionnees = string.Join(",", listeCouleurs);
                }

                // حساب الأسعار بناءً على اختيار TVA
                decimal prixHT = priceBox.Value;
                decimal tauxTVA = applyTvaCheckBox.Checked ? tvaRateBoxRef.Value : 0;
                decimal prixTTC;

                if (applyTvaCheckBox.Checked)
                {
                    prixTTC = prixHT * (1 + (tauxTVA / 100));
                }
                else
                {
                    prixTTC = prixHT; // بدون TVA
                }

                string query = @"INSERT INTO products 
                        (name, price, stock_quantity, size, color, category, barcode, 
                         tva_rate, price_without_tva, created_at, updated_at) 
                        VALUES (@name, @price, @quantity, @size, @color, @category, @barcode, 
                                @tva_rate, @price_without_tva, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@name", nameBox.Text.Trim());
                cmd.Parameters.AddWithValue("@price", prixTTC); // تخزين السعر TTC
                cmd.Parameters.AddWithValue("@quantity", (int)quantityBox.Value);
                cmd.Parameters.AddWithValue("@size", string.IsNullOrEmpty(taillesSelectionnees) ? DBNull.Value : (object)taillesSelectionnees);
                cmd.Parameters.AddWithValue("@color", string.IsNullOrEmpty(couleursSelectionnees) ? DBNull.Value : (object)couleursSelectionnees);
                cmd.Parameters.AddWithValue("@category", string.IsNullOrWhiteSpace(categoryBox.Text) ? "Général" : categoryBox.Text.Trim());
                cmd.Parameters.AddWithValue("@barcode", string.IsNullOrEmpty(codeBarres) ? DBNull.Value : (object)codeBarres);
                cmd.Parameters.AddWithValue("@tva_rate", tauxTVA);
                cmd.Parameters.AddWithValue("@price_without_tva", prixHT); // تخزين السعر HT

                cmd.ExecuteNonQuery();

                string messageSucces = "✅ PRODUIT AJOUTÉ AVEC SUCCÈS!\n\n" +
                                      $"📦 NOM: {nameBox.Text}\n" +
                                      $"💰 PRIX HT: {prixHT:N2} €\n" +
                                      $"🛒 PRIX TTC: {prixTTC:N2} €\n" +
                                      $"📦 QUANTITÉ: {quantityBox.Value}";

                if (applyTvaCheckBox.Checked)
                {
                    decimal montantTVA = prixTTC - prixHT;
                    messageSucces += $"\n📊 TVA ({tauxTVA}%): {montantTVA:N2} €";
                }

                if (!string.IsNullOrEmpty(taillesSelectionnees))
                    messageSucces += $"\n📏 TAILLES: {taillesSelectionnees}";
                if (!string.IsNullOrEmpty(couleursSelectionnees))
                    messageSucces += $"\n🎨 COULEURS: {couleursSelectionnees}";

                MessageBox.Show(messageSucces, "SUCCÈS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERREUR: {ex.Message}", "ERREUR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenererCodeBarres()
        {
            try
            {
                Random random = new Random();
                string codeBarresBase = "20" + random.Next(100000000, 999999999).ToString();
                if (codeBarresBase.Length != 11)
                    codeBarresBase = "20" + DateTime.Now.Ticks.ToString().Substring(0, 9);

                int somme = 0;
                for (int i = 0; i < 11; i++)
                {
                    if (i < codeBarresBase.Length)
                    {
                        int chiffre = int.Parse(codeBarresBase[i].ToString());
                        somme += (i % 2 == 0) ? chiffre : chiffre * 3;
                    }
                }
                int chiffreControle = (10 - (somme % 10)) % 10;
                return codeBarresBase + chiffreControle;
            }
            catch
            {
                return "AUTO" + DateTime.Now.Ticks.ToString().Substring(0, 10);
            }
        }
    }

    // Classe pour boutons personnalisés
    public class CustomButton : Button
    {
        public CustomButton(string text, int x, int y, int width, int height, Color? color = null)
        {
            this.Text = text;
            this.Location = new Point(x, y);
            this.Size = new Size(width, height);
            this.BackColor = color ?? Color.FromArgb(59, 130, 246);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            this.FlatStyle = FlatStyle.Flat;
            this.Cursor = Cursors.Hand;
            this.FlatAppearance.BorderSize = 0;
            this.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(this.BackColor.R + 20, 255),
                Math.Min(this.BackColor.G + 20, 255),
                Math.Min(this.BackColor.B + 20, 255)
            );
        }
    }
}