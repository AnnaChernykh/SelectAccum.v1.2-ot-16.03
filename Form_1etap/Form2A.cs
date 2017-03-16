using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;

namespace Form_1etap
{
    public partial class Form2A : Form
    {
        ToolStripLabel infoLabel;
        Boolean z = false;
        public Form2A()
        {
            InitializeComponent();
            infoLabel = new ToolStripLabel();
            statusStrip1.Items.Add(infoLabel);
            infoLabel.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            //Создается таблица в БД, имена и тип столбцов
            textBox1.ReadOnly = true;
            string TableName = textBox1.Text;
            string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");

            try
            {
                label2.Text = "";
                OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
                myOleDbConnection.Open();
                OleDbCommand CreateTable = myOleDbConnection.CreateCommand();
                CreateTable.CommandText = string.Format("CREATE TABLE " + TableName + " (Type char(60) not null" +
               ", Nominal_capacity int not null, Internal_resistance double not null) ", myOleDbConnection);
                OleDbDataReader myOleDbDataReader2 = CreateTable.ExecuteReader();
                myOleDbDataReader2.Read();
                myOleDbDataReader2.Close();
                myOleDbConnection.Close();
                infoLabel.Text = "Таблица создана";
                textBox1.ReadOnly = true;

            }
            catch
            { infoLabel.Text = "Таблица не создана"; }

            //Созданная пустая таблица открывается в ДатаГридВью для заполнения
            try
            {
                label2.Text = "";
                string sql = "SELECT * FROM " + TableName;
                OleDbConnection connection = new OleDbConnection(connectionString);
                OleDbDataAdapter dataadapter = new OleDbDataAdapter(sql, connection);
                OleDbCommandBuilder cb = new OleDbCommandBuilder(dataadapter);
                DataSet ds = new DataSet();
                connection.Open();
                dataadapter.Fill(ds, TableName);
                connection.Close();
                dataGridView1.DataSource = ds;
                dataGridView1.DataMember = TableName;

                button2.Click += (senderSlave, eSlave) =>
                {
                    if (dataGridView1.RowCount == 0)
                    { MessageBox.Show("Пустая таблица.Заполните!"); }
                    else
                    {
                        int doNotWrite = 0;

                        for (int j = 0; j < dataGridView1.Rows.Count - 1; j++)
                            for (int i = 0; i < 3; i++)
                                if (string.IsNullOrEmpty(dataGridView1.Rows[j].Cells[i].Value.ToString()))
                                {
                                    doNotWrite = 1;
                                }

                        if (doNotWrite == 1)
                        {
                            MessageBox.Show("Заполнены не все ячейки!Чтобы сохранить \nвведенные данные, заполните все строки.");
                            infoLabel.Text = "Изменения в таблице не сохранены";
                        }
                        else
                        {
                            dataadapter.InsertCommand = cb.GetInsertCommand();
                            dataadapter.Update(ds, TableName);
                            infoLabel.Text = "Изменения в таблице сохранены";
                        }
                    }
                };
                button3.Click += (senderSlave2, eSlave2) =>
                {
                    if (dataGridView1.RowCount == 0)
                    {
                        MessageBox.Show("Пустая таблица.Заполните!");
                    }
                    else
                    {
                        int doNotWrite = 0;

                        for (int j = 0; j < dataGridView1.Rows.Count - 1; j++)
                            for (int i = 0; i < 3; i++)
                                if (string.IsNullOrEmpty(dataGridView1.Rows[j].Cells[i].Value.ToString()))
                                {
                                    doNotWrite = 1;
                                }

                        if (doNotWrite == 1)
                        {
                            MessageBox.Show("Заполнены не все ячейки!Чтобы сохранить \nвведенные данные, заполните все строки.");
                            infoLabel.Text = "Изменения в таблице не сохранены";
                        }
                        else
                        {
                            dataadapter.InsertCommand = cb.GetInsertCommand();
                            dataadapter.Update(ds, TableName);
                            infoLabel.Text = "Изменения в таблице сохранены";

                        }
                    }
                };
            }
            catch (DataException)
            {
                label2.Text = "Неверный формат данных";
                infoLabel.Text = "Таблица не сохранена";
            }
            catch
            {
                label2.Text = "Ошибка при открытии.Некорректное имя таблицы.";
                textBox1.ReadOnly = false;
                infoLabel.Text = "Таблица не сохранена";
            }

        }

        private void Form2A_Load(object sender, EventArgs e)
        {
            
        }
        private void DataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs anError)
        {
            // MessageBox.Show("Error happened: " + anError.Context.ToString()); - выдает сообщения стандартными "parsing", "commit"
            if (anError.Context.ToString().Contains(DataGridViewDataErrorContexts.Commit.ToString()) ||
                anError.Context.ToString().Contains(DataGridViewDataErrorContexts.CurrentCellChange.ToString()) ||
                anError.Context.ToString().Contains(DataGridViewDataErrorContexts.Parsing.ToString()))
            {
                MessageBox.Show("ОШИБКА: неверный формат введенных данных.");
            }

        }
        private void Form2_Closing(object sender, FormClosingEventArgs e)
        {
            Form3A form3A = new Form3A();
            form3A.Owner = this;
            if (z == false)
            {
                if (MessageBox.Show("Вы действительно хотите закрыть окно? \nСохраненные данные не удалятся", "Выход",
                  MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form1 main = this.Owner as Form1;
            Form3A form3A = new Form3A();
            form3A.Owner = this;
            textBox1.ReadOnly = false;
            string TableName = textBox1.Text;

            int U = Convert.ToInt32(maskedTextBox5.Text);
            int N = Convert.ToInt32(Math.Round(1.05 * U / 2.23));
            double Unmr = (0.85 * U + 0.04 * U) / N;
            form3A.label1.Text = "Введите разрядную характеристику для наименьшего рабочего напряжения,\nблизкого к значению "+Convert.ToString(Math.Round(Unmr,2));

            string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");
            form3A.Show();

            // закрыли вторую форму, открыли третью, создаем табл с разряд.хар-кой, выводим ее в датаГридВью

            string TableName1 = "CCD_" + textBox1.Text;
            try
            {
                OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
                myOleDbConnection.Open();
                OleDbCommand CreateTable = myOleDbConnection.CreateCommand();
                CreateTable.CommandText = string.Format("CREATE TABLE " + TableName1 + " (Type char(60) not null" +
               ", ShortCurrent double not null, LongCurrent double not null)", myOleDbConnection);
                OleDbDataReader myOleDbDataReader2 = CreateTable.ExecuteReader();
                myOleDbDataReader2.Read();
                myOleDbDataReader2.Close();
                myOleDbConnection.Close();
                form3A.toolStripStatusLabel1.Text = "Таблица создана";
                //копирование столбца Тайп в таблицу ССД
                OleDbConnection myOleDbConnection2 = new OleDbConnection(connectionString);
                myOleDbConnection2.Open();
                OleDbCommand InsertType = myOleDbConnection2.CreateCommand();
                InsertType.CommandText = string.Format("INSERT INTO " + TableName1 + "(Type) SELECT Type FROM "+ textBox1.Text, myOleDbConnection2);
                OleDbDataReader myOleDbDataReader3 = InsertType.ExecuteReader();
                myOleDbDataReader3.Read();
                myOleDbDataReader3.Close();
                myOleDbConnection2.Close();

            }
            catch
            {
                form3A.toolStripStatusLabel1.Text = "Таблица не создана";
            }

            //Созданная пустая таблица открывается в ДатаГридВью для заполнения
            try
            {
                string sql = "SELECT * FROM " + TableName1;
                OleDbConnection connection = new OleDbConnection(connectionString);
                OleDbDataAdapter dataadapter = new OleDbDataAdapter(sql, connection);
                OleDbCommandBuilder cb = new OleDbCommandBuilder(dataadapter);
                DataSet ds = new DataSet();
                connection.Open();
                dataadapter.Fill(ds, TableName1);
                connection.Close();
                form3A.dataGridView1.DataSource = ds;
                form3A.dataGridView1.DataMember = TableName1;

                form3A.button2.Click += (senderSlave, eSlave) =>
                {
                    if (form3A.dataGridView1.RowCount == 0)
                    { MessageBox.Show("Пустая таблица.Заполните!"); }
                    else
                    {
                        int doNotWrite = 0;

                        for (int j = 0; j < form3A.dataGridView1.Rows.Count - 1; j++)
                            for (int i = 0; i < 3; i++)
                                if (string.IsNullOrEmpty(form3A.dataGridView1.Rows[j].Cells[i].Value.ToString()))
                                {
                                    doNotWrite = 1;
                                }

                        if (doNotWrite == 1)
                        {
                            MessageBox.Show("Заполнены не все ячейки!Чтобы сохранить \nвведенные данные, заполните все строки.");
                            form3A.toolStripStatusLabel1.Text = "Изменения в таблице не сохранены";
                        }
                        else
                        {
                            dataadapter.InsertCommand = cb.GetInsertCommand();
                            dataadapter.Update(ds, TableName1);
                            form3A.toolStripStatusLabel1.Text = "Изменения в таблице сохранены";
                        }
                    }
                };
                form3A.button3.Click += (senderSlave2, eSlave2) =>
                {
                    if (form3A.dataGridView1.RowCount == 0)
                    {
                        MessageBox.Show("Пустая таблица.Заполните!");
                    }
                    else
                    {
                        int doNotWrite = 0;

                        for (int j = 0; j < form3A.dataGridView1.Rows.Count - 1; j++)
                            for (int i = 0; i < 3; i++)
                                if (string.IsNullOrEmpty(form3A.dataGridView1.Rows[j].Cells[i].Value.ToString()))
                                {
                                    doNotWrite = 1;
                                }

                        if (doNotWrite == 1)
                        {
                            MessageBox.Show("Заполнены не все ячейки!Чтобы сохранить \nвведенные данные, заполните все строки.");
                            form3A.toolStripStatusLabel1.Text = "Изменения в таблице не сохранены";
                        }
                        else
                        {
                            dataadapter.InsertCommand = cb.GetInsertCommand();
                            dataadapter.Update(ds, TableName1);
                            this.Close();
                            z = true;
                            form3A.Close();

                        }
                    }
                };
            }
            catch (DataException)
            {
                label2.Text = "Неверный формат данных";
                form3A.toolStripStatusLabel1.Text = "Таблица не сохранена";
            }
            catch
            {
                label2.Text = "Ошибка при открытии.Некорректное имя таблицы.";
                form3A.toolStripStatusLabel1.Text = "Таблица не сохранена";
            }
        
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }
    }
    }

