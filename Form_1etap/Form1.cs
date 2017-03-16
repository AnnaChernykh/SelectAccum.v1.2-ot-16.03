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
    public partial class Form1 : Form
    {
        //1. Отредактировать расчет потерь напряжения (зачем материал, сечение; вводные секции?; добавить расчтет до ЭП)
        //2. Расчет доп.группы не адаптирован под вновь вводимые таблицы
        //3. Нет возможности ввести два типа пластин в одну таблицу, надо добавлять окно ввода крит.емкости
        //4. Обработать ошибку не ввода напряжения в окне 2А
        //5. Обработать событие, когда сохраняют пустую таблицу 
        //6. Копировать столбец "Тип" в ССД для соответствия типов и упрощения заполнения
        //7. Пересчет емкости по принципу равенства емкостей, без критических значений - СДЕЛАНО
        //8. Добавить возможность сохранить результат в пдф
        //9. Картиночки
        Form2A form2A = new Form2A();
        Form3A form3A = new Form3A();
        public Form1()
        {
            form2A.Owner = this;
            form2A.Hide();
            form3A.Owner = this;
            form3A.Hide();
            InitializeComponent();
            string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");
            OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
            myOleDbConnection.Open();
            DataTable tbls = myOleDbConnection.GetSchema("Tables", new string[] { null, null, null, "TABLE" });
            //исключаем из выпадающего списка таблицы с разряд.характеристикой (они содержат в имене CCD)
            foreach (DataRow row in tbls.Rows)
            {
                string TableName = row["TABLE_NAME"].ToString();
                if (!TableName.Contains("CCD"))
                { comboBox1.Items.Add(TableName); }
            }
            myOleDbConnection.Close();
            comboBox1.Items.Add("Ввести новый");
            comboBox1.SelectedItem = "GroE";
            radioButton2.Checked = true;

        }
        /// <summary>
        /// Расчет емкости аккумулятора (учитывает установку/отсутствие устр-ва стабилизаци)
        /// </summary>
        /// <param name="Iprod"></param>
        /// <param name="Ikratk"></param>
        /// <param name="Tprod"></param>
        /// <param name="Tkratk"></param>
        /// <param name="Unmr"></param>
        /// <param name="D"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public static int GetCapacity(int Iprod, int Ikratk, int Tprod, int Tkratk, double Unmr, Boolean D, string TableName)  //по Unmr надо выбирать расчетные хар-ки
        {  
            double Cpred, Cpred2, K1, C, K2, DischargeLong, DischargeShort, dVmin, UnmrStandart, dV;
            string TypeS, timeK, timeP, TableNameOfCCD; 
            int d, dmin, Cshtrih, Cmin;
            Boolean z1=true; //1-нужен пересчет, 0-не нужен

            C = 1.5 * Iprod * Tprod / 60;

            string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");
            OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
            OleDbCommand FindTypeC = myOleDbConnection.CreateCommand();
            FindTypeC.CommandText = string.Format("SELECT Type, Nominal_capacity FROM "+TableName);
            myOleDbConnection.Open();
            OleDbDataReader myOleDbDataReader1 = FindTypeC.ExecuteReader();
            myOleDbDataReader1.Read();
            dmin = Math.Abs(Convert.ToInt32(myOleDbDataReader1["Nominal_capacity"]) - Convert.ToInt32(Math.Ceiling(C)));
            TypeS = Convert.ToString(myOleDbDataReader1["Type"]);
            Cshtrih = Convert.ToInt32(myOleDbDataReader1["Nominal_capacity"]);
            while (myOleDbDataReader1.Read())
            {
                Cmin = Convert.ToInt32(myOleDbDataReader1["Nominal_capacity"]);
                d = Math.Abs(Cmin - Convert.ToInt32(Math.Ceiling(C)));
                if (d < dmin)
                {
                    dmin = d;
                    TypeS = Convert.ToString(myOleDbDataReader1["Type"]);
                    Cshtrih = Cmin;
                }
                if (Cmin > C)
                { break; }
            }
           
            myOleDbDataReader1.Close();

            //если выбрана табл. ГроЕ, то подбираем разряд.характеристику по расчетному напряжению, для вновь вводимых она одна
            if (TableName == "GroE")
            {

                double[] StandartVoltage = { 1.700, 1.750, 1.775, 1.800, 1.825, 1.850, 1.875, 1.900 };  //стандартное напряжение для разряд.хар-к
                dVmin = Math.Abs(StandartVoltage[1] - Unmr);  // поиск ближайшего напряжения в конце разряда из ряда стандратных
                UnmrStandart = StandartVoltage[1];
                foreach (double v in StandartVoltage)
                {
                    dV = Math.Abs(v - Unmr);
                    if (dVmin > dV)
                    {
                        dVmin = dV;
                        UnmrStandart = v;
                    }
                    if (Unmr < v)
                        break;
                }
                TableNameOfCCD = Convert.ToString(Convert.ToInt32(UnmrStandart * 1000));
            }
            else { TableNameOfCCD = TableName; }
                do
                {
                    z1 = true;
                    OleDbCommand FindK12 = myOleDbConnection.CreateCommand();
                    FindK12.CommandText = string.Format("SELECT min" + Tprod + " ,min" + Tkratk + " FROM CCD_" + TableNameOfCCD + " WHERE Type='" + TypeS + "'");
                    OleDbDataReader myOleDbDataReader2 = FindK12.ExecuteReader();
                    myOleDbDataReader2.Read();
                    timeK = "min" + Tkratk;
                    timeP = "min" + Tprod;

                    DischargeShort = Convert.ToDouble(myOleDbDataReader2[timeK]);
                    DischargeLong = Convert.ToDouble(myOleDbDataReader2[timeP]);
                    myOleDbDataReader2.Close();

                    K1 = Cshtrih / DischargeLong;
                    K2 = Cshtrih / DischargeShort;

                    if (D == false)
                    { Cpred = K1 * Iprod + K2 * Ikratk; }
                    else
                    { Cpred = K1 * Iprod; }

                    OleDbCommand FindTypeCpred = myOleDbConnection.CreateCommand();
                    FindTypeCpred.CommandText = string.Format("SELECT Type, Nominal_capacity FROM GroE WHERE Nominal_capacity>=" + Convert.ToInt32(Cpred) + " ORDER BY Capacity");
                    OleDbDataReader myOleDbDataReader3 = FindTypeCpred.ExecuteReader();
                    myOleDbDataReader3.Read();
                    TypeS = Convert.ToString(myOleDbDataReader3["Type"]);
                    Cshtrih = Convert.ToInt32(myOleDbDataReader3["Nominal_capacity"]);
                    myOleDbDataReader3.Close();

                    FindK12 = myOleDbConnection.CreateCommand();
                    FindK12.CommandText = string.Format("SELECT min" + Tprod + " ,min" + Tkratk + " FROM CCD_" + TableNameOfCCD + " WHERE Type='" + TypeS + "'");
                    OleDbDataReader myOleDbDataReader4 = FindK12.ExecuteReader();
                    myOleDbDataReader4.Read();
                    timeK = "min" + Tkratk;
                    timeP = "min" + Tprod;

                    DischargeShort = Convert.ToDouble(myOleDbDataReader4[timeK]);
                    DischargeLong = Convert.ToDouble(myOleDbDataReader4[timeP]);
                    myOleDbDataReader4.Close();

                    K1 = Cshtrih / DischargeLong;
                    K2 = Cshtrih / DischargeShort;

                    if (D == false)
                    { Cpred2 = K1 * Iprod + K2 * Ikratk; }
                    else
                    { Cpred2 = K1 * Iprod; }

                    if (Cpred==Cpred2)
                    { z1 = false; }
                    
                }
                while (z1==true);
                myOleDbConnection.Close();
                int Сkon = Convert.ToInt32(Math.Ceiling(1.5 * Cpred2));
                return Сkon;
        }
/// <summary>
/// Поиск типа аккумулятора по рассчитанной емкости в соотвующей БД
/// </summary>
/// <param name="C"></param>
/// <param name="TableName"></param>
/// <returns></returns>
        public static string GetType(int C,string TableName)
        {//ПП, которая определяет тип аккумулятора по емкости
            string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");
            OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
            OleDbCommand myOleDbCommand = myOleDbConnection.CreateCommand();
            myOleDbCommand.CommandText = string.Format("SELECT Type FROM "+TableName+" WHERE Nominal_capacity>=" + C + " ORDER BY Nominal_capacity");
            myOleDbConnection.Open();
            OleDbDataReader myOleDbDataReader = myOleDbCommand.ExecuteReader();
            myOleDbDataReader.Read();
            string TypeS = Convert.ToString(myOleDbDataReader["Type"]);
            myOleDbDataReader.Close();
            myOleDbConnection.Close();
            return TypeS;
        }
        /// <summary>
        /// Расчет емкости аккумулятора с учетом доп.группы
        /// </summary>
        /// <param name="Iprod"></param>
        /// <param name="Ikratk"></param>
        /// <param name="Tprod"></param>
        /// <param name="Tkratk"></param>
        /// <param name="U"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public static string[] DopGroup(int Iprod, int Ikratk, int Tprod, int Tkratk, double U, string TableName)
        {
            int N, C, Ctekush, Ndop, d, dmin, Cshtrih;
            double Uz, Unmr, R, IPrivod, UakumKrit, DischargeLong, K1, dVmin, UnmrStandart, dV, Cpred, Cpred2;
            string[] ArrayOfCharacter = new string[4];
            string TypeS, timeP, TableNameOfCCD;
            bool z1 = false;  //indikator perehoda na plastini GroE100

            N = Convert.ToInt32(Math.Round(1.05 * U / 2.23));
            Uz = 1.1 * U / N;
            Unmr = (0.85 * U + 0.04 * U) / N;

            C = Convert.ToInt32(Math.Ceiling(1.5 * Iprod * Tprod / 60));
            string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");
            OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
            OleDbCommand FindTypeCpred = myOleDbConnection.CreateCommand();
            FindTypeCpred.CommandText = string.Format("SELECT Type, Nominal_capacity,Internal_resistance FROM "+TableName);
            myOleDbConnection.Open();
            OleDbDataReader myOleDbDataReader1 = FindTypeCpred.ExecuteReader();
            myOleDbDataReader1.Read();
            Ctekush = Convert.ToInt32(myOleDbDataReader1["Nominal_capacity"]);
            dmin = Math.Abs(Ctekush - C);
            TypeS = Convert.ToString(myOleDbDataReader1["Type"]);
            R = Convert.ToDouble(myOleDbDataReader1["Internal_resistance"]);
            while (myOleDbDataReader1.Read())
            {
                Ctekush = Convert.ToInt32(myOleDbDataReader1["Nominal_capacity"]);
                d = Math.Abs(Ctekush - C);
                if (d < dmin)
                {
                    dmin = d;
                    TypeS = Convert.ToString(myOleDbDataReader1["Type"]);
                    R = Convert.ToDouble(myOleDbDataReader1["Internal_resistance"]);
                    C = Ctekush;
                }
            }
            myOleDbDataReader1.Close();



            IPrivod = Ikratk * 0.85 * U / U;
            UakumKrit = Unmr - 1.5 * IPrivod * R * 0.001;
            Ndop = Convert.ToInt32(Math.Round((0.85 * U + 0.04 * U) / UakumKrit));
            if (0.7 * U > Ndop * UakumKrit)
            {
                R = (Unmr - 0.7 * U / Ndop) / (1.5 * IPrivod);

                OleDbCommand FindCapacityPlusR = myOleDbConnection.CreateCommand();
                FindCapacityPlusR.CommandText = string.Format("SELECT Nominal_capacity,Type,Internal_resistance FROM "+TableName+" WHERE Internal_resistance<='" + Convert.ToString(R) + "' AND Nominal_capacity>=" + C + " ORDER BY Capacity");
                OleDbDataReader myOleDbDataReader2 = FindCapacityPlusR.ExecuteReader();
                myOleDbDataReader2.Read();
                C = Convert.ToInt32(myOleDbDataReader2["Nominal_capacity"]);
                TypeS = Convert.ToString(myOleDbDataReader2["Type"]);
                R = Convert.ToDouble(myOleDbDataReader2["Internal_resistance"]);
                myOleDbDataReader2.Close();

            }
            if ((Ndop * 2.05 - 0.04 * U) > 1.1 * U)
            {
                Ndop = Convert.ToInt32(Math.Truncate((1.1 * U + 0.04 * U) / 2.05));
            }


            Unmr = (0.85 * U + 0.04 * U) / Ndop + 1.5 * IPrivod * R * 0.001;

            double[] StandartVoltage = { 1.700, 1.750, 1.775, 1.800, 1.825, 1.850, 1.875, 1.900 };  //стандартное напряжение для разряд.хар-к
            dVmin = Math.Abs(StandartVoltage[1] - Unmr);  // поиск ближайшего напряжения в конце разряда из ряда стандратных
            UnmrStandart = StandartVoltage[1];
            foreach (double v in StandartVoltage)
            {
                dV = Math.Abs(v - Unmr);
                if (dVmin > dV)
                {
                    dVmin = dV;
                    UnmrStandart = v;
                }
                if (Unmr < v)
                    break;
            }
            if (TableName=="GroE")
            { TableNameOfCCD = Convert.ToString(UnmrStandart * 1000); }
            else { TableNameOfCCD = TableName; }
            do
            {
                z1 = true;
                OleDbCommand FindK12 = myOleDbConnection.CreateCommand();
                FindK12.CommandText = string.Format("SELECT min" + Tprod + " ,min" + Tkratk + " FROM CCD_" + TableNameOfCCD + " WHERE Type='" + TypeS + "'");
                OleDbDataReader myOleDbDataReader2 = FindK12.ExecuteReader();
                myOleDbDataReader2.Read();
                timeP = "min" + Tprod;

                DischargeLong = Convert.ToDouble(myOleDbDataReader2[timeP]);
                myOleDbDataReader2.Close();

                K1 = C / DischargeLong;                       
                Cpred = K1 * Iprod; 

                OleDbCommand FindTypeCpredDop = myOleDbConnection.CreateCommand();
                FindTypeCpredDop.CommandText = string.Format("SELECT Type, Nominal_capacity FROM "+TableName+" WHERE Nominal_capacity>=" + Convert.ToInt32(Cpred) + " ORDER BY Capacity");
                OleDbDataReader myOleDbDataReader3 = FindTypeCpredDop.ExecuteReader();
                myOleDbDataReader3.Read();
                TypeS = Convert.ToString(myOleDbDataReader3["Type"]);
                Cshtrih = Convert.ToInt32(myOleDbDataReader3["Nominal_capacity"]);
                myOleDbDataReader3.Close();

                FindK12 = myOleDbConnection.CreateCommand();
                FindK12.CommandText = string.Format("SELECT min" + Tprod + " ,min" + Tkratk + " FROM CCD_" + TableNameOfCCD + " WHERE Type='" + TypeS + "'");
                OleDbDataReader myOleDbDataReader4 = FindK12.ExecuteReader();
                myOleDbDataReader4.Read();
                timeP = "min" + Tprod;

                DischargeLong = Convert.ToDouble(myOleDbDataReader4[timeP]);
                myOleDbDataReader4.Close();

                K1 = Cshtrih / DischargeLong;
                           
                Cpred2 = K1 * Iprod; 

                if (Cpred == Cpred2)
                { z1 = false; }
            }
            while (z1 ==true);

            int Сkon = Convert.ToInt32(Math.Ceiling(1.5 * Cpred2));

            OleDbCommand myOleDbCommand = myOleDbConnection.CreateCommand();
            myOleDbCommand.CommandText = string.Format("SELECT Type FROM "+TableName+" WHERE Nominal_capacity>=" + Сkon);
            OleDbDataReader myOleDbDataReader5 = myOleDbCommand.ExecuteReader();
            myOleDbDataReader5.Read();
            TypeS = Convert.ToString(myOleDbDataReader5["Type"]);
            myOleDbDataReader5.Close();
            myOleDbConnection.Close();

            ArrayOfCharacter[0] = Convert.ToString(Сkon);
            ArrayOfCharacter[1] = Convert.ToString(Ndop);
            ArrayOfCharacter[2] = TypeS;
            ArrayOfCharacter[3] = "Расчет с дополнительной группой проведен."
                               + "\nРекомендаций нет! ";
            return ArrayOfCharacter;

        }




        private void Form1_Load(object sender, EventArgs e)
        {
            
            form3A.button3.Click += (senderSlave, eSlave) =>
            {
                comboBox1.Items.Add(form2A.textBox1.Text);
                comboBox1.Update();
            };
            
        }
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        public void button1_Click(object sender, EventArgs e)
        {
            double Uz, Unmr, dU;

            int C, N, U, Iprod, Ikratk, TprodVvod, TkratkVvod, Tprod, Tkratk, dmin, d;
            string TypeS, TableName1 = Convert.ToString(comboBox1.SelectedItem);
            Boolean StabRaschet = false, SoglasieNaDopRaschet = false, D = false;
            double ResistanceAB_SHVAB,LengthAB_SHVAB, ResistanceSHVAB_SHPT, LengthSHVAB_SHPT;

            label2.Hide();
            label8.Hide();
            label7.Hide();
            label9.Hide();
            label10.Hide();
            label22.Hide();
            label24.Hide();

            Form2 form2 = new Form2();
            form2.Owner = this;
            form2.Hide();

            Form3 form3 = new Form3();
            form3.Owner = this;
            form3.Hide();

            try
            {
                U = Convert.ToInt32(maskedTextBox5.Text);
                Iprod = Convert.ToInt32(maskedTextBox1.Text);
                Ikratk = Convert.ToInt32(maskedTextBox2.Text);
                TprodVvod = Convert.ToInt32(maskedTextBox3.Text);
                TkratkVvod = Convert.ToInt32(maskedTextBox4.Text);
               
                int[] StandartTime = { 1, 5, 10, 15, 20, 30, 60, 180, 300, 600 };  //стандартное время в разряд.хар-х
                dmin = Math.Abs(StandartTime[1] - TprodVvod);  // поиск ближайшего времени для постоянной нагрузки из ряда стандратных
                Tprod = StandartTime[1];
                foreach (int ts in StandartTime)
                {
                    d = Math.Abs(ts - TprodVvod);
                    if (dmin > d)
                    {
                        dmin = d;
                        Tprod = ts;
                    }
                    if (TprodVvod < ts)
                        break;
                }

                dmin = Math.Abs(StandartTime[1] - TkratkVvod);  //поиск ближайшего времени для кратковременной нагрузки из ряда стандартных
                Tkratk = StandartTime[1];
                foreach (int ts in StandartTime)
                {
                    d = Math.Abs(ts - TkratkVvod);
                    if (dmin > d)
                    {
                        dmin = d;
                        Tkratk = ts;
                    }
                    if (TkratkVvod < ts)
                        break;
                }

          

                if (checkBox2.Checked == true)
                {


                    form3.button1.Click += (senderSlave, eSlave) =>
                    {
                        SoglasieNaDopRaschet = true;
                    };
                    form3.button2.Click += (senderSlave, eSlave) =>
                    {
                        SoglasieNaDopRaschet = false;
                    };

                    if (checkBox1.Checked == true)
                    {
                        form3.label1.Text = "  При наличии устаревших выключателей рекомендуется "
                                      + "\nпровести расчет с учетом установки дополнительной"
                                      + "\nгруппы аккумуляторов. При этом устройство стабилизации"
                                      + "\nне рассматривается. Продолжить расчет с дополнительной группой?";
                        form3.button1.Text = "Расчет с учетом доп.группы";
                        form3.button2.Text = "Расчет с учетом устройства стабилизации";
                    }
                    else
                    {
                        form3.label1.Text = "  Так как имеются устаревшие выключатели, рекомендуем "
                                        + "\nустановить дополнительную группу аккумуляторов."
                                        + "\nПродолжить расчет с дополнительной группой?";
                    }

                    form3.ShowDialog();

                }

                if (radioButton1.Checked==true)
                {
                    LengthAB_SHVAB = Convert.ToDouble(maskedTextBox6.Text);
                    ResistanceAB_SHVAB = Convert.ToDouble(maskedTextBox7.Text);
                    //SquareAB_SHVAB = Convert.ToDouble(maskedTextBox8.Text);
                    double dU1 = 2 * LengthAB_SHVAB * ResistanceAB_SHVAB * Iprod*0.001;

                    LengthSHVAB_SHPT = Convert.ToDouble(maskedTextBox13.Text);
                    ResistanceSHVAB_SHPT = Convert.ToDouble(maskedTextBox12.Text);
                   // SquareSHVAB_SHPT = Convert.ToDouble(maskedTextBox8.Text);
                    double dU2 = 2 * LengthSHVAB_SHPT * ResistanceSHVAB_SHPT * Iprod*0.001;
                    dU = dU1 + dU2;
                }
                else
                {
                     dU = 0.04 * U;
                }

                if (SoglasieNaDopRaschet == true)
                {
                    string[] ArrayOfCharacter = new string[4];
                    ArrayOfCharacter = DopGroup(Convert.ToInt32(Iprod), Convert.ToInt32(Ikratk), Convert.ToInt32(Tprod), Convert.ToInt32(Tkratk), U, TableName1);
                    form2.label2.Text = ArrayOfCharacter[0];
                    form2.label5.Text = ArrayOfCharacter[1];
                    form2.label8.Text = ArrayOfCharacter[2];
                    form2.label7.Text = ArrayOfCharacter[3];
                    form2.button1.Hide();
                    form2.ShowDialog();

                }

                else
                {
                    if (checkBox1.Checked == false)
                    {
                        N = Convert.ToInt32(Math.Round(1.05 * U / 2.23));      //количество аккумуляторов, округление до целого
                        Uz = 1.1 * U / N;                            //U ускоренного заряда
                        Unmr = (0.85 * U + dU) / N;          //U наим.рабочее
                        D = false;
                        C = GetCapacity(Convert.ToInt32(Iprod), Convert.ToInt32(Ikratk), Convert.ToInt32(Tprod), Convert.ToInt32(Tkratk), Unmr, D,TableName1);
                        TypeS = GetType(C, TableName1);
                        if (Unmr > 1.9)
                        {

                            form2.button1.Click += (senderSlave, eSlave) =>
                          {
                              StabRaschet = true;
                          };


                            form2.label2.Text = Convert.ToString(C);
                            form2.label5.Text = Convert.ToString(N);
                            form2.label8.Text = TypeS;
                            form2.label7.Text = "  Расчетное напряжение на аккумуляторе в конце разряда составляет более 1,9 В,"
                                      + "\nнеобходимо предпринять мероприятия по снижению потери напряжения в цепи между АБ"
                                      + "\nи электроприемником путем реализации следующих мероприятий:"
                                      + "\n а) увеличение сечения кабелей в цепи питания электроприемников;"
                                      + "\n б) уменьшение тока нагрузки за счет запрета одновременной работы электроприемников;"
                                      + "\nнапример, запрет одновременного завода пружин приводов выключателей комплектного"
                                      + "\nраспределительного устройства;"
                                      + "\n в) увеличить количество аккумуляторов в батарее путем установки в СОПТ устройств"
                                      + "\nстабилизации напряжения."
                                      + "\n  Устройства стабилизации напряжения повышают коэффициент использования емкости"
                                      + "\nаккумуляторной батареи.Возможность их установки может быть рассмотрена независимо"
                                      + "\nот значения расчетного напряжения на аккумуляторе в конце разряда. Unmr=" + Unmr;
                            form2.button1.Text = "Рассчитать устройство стабилизации";
                            form2.ShowDialog();

                            if (StabRaschet == true)
                            {
                                N = Convert.ToInt32(Math.Round(1.1 * U / 2.23));
                                Uz = 1.1 * U / N;
                                Unmr = (0.85 * U + dU) / N;
                                D = false;
                                C = GetCapacity(Convert.ToInt32(Iprod), Convert.ToInt32(Ikratk), Convert.ToInt32(Tprod), Convert.ToInt32(Tkratk), Unmr, D,TableName1);
                                TypeS = GetType(C,TableName1);
                                if (Unmr > 1.9)
                                {


                                    form2.label2.Text = Convert.ToString(C);
                                    form2.label5.Text = Convert.ToString(N);
                                    form2.label8.Text = TypeS;
                                    form2.label7.Text = "  Установка устройства стабилизации не позволяет обеспечить напряжение на аккумуляторах"
                                      + "\nв конце разряда ниже 1,9 В, что может иметь место на реконструируемых подстанциях"
                                      + "\nс выключателями, оборудованными электромагнитными приводами прямого действия."
                                      + "\nРекомендуется рассмотреть возможность организации дополнительной группы аккумуляторов"
                                      + "\nв батарее для компенсации потери напряжения в цепи питания кратковременной нагрузки. Unmr = " + Unmr;

                                }
                                else
                                {
                                    form2.label2.Text = Convert.ToString(C);
                                    form2.label5.Text = Convert.ToString(N);
                                    form2.label8.Text = TypeS;
                                    form2.label7.Text = "  Установка устройства стабилизации обеспечивает напряжение на аккумуляторах"
                                      + "\nв конце разряда ниже 1,9 В. Рекомендаций нет. Unmr = " + Unmr;
                                    form2.button1.Hide();
                                    form2.ShowDialog();
                                }
                            }
                        }
                        else
                        {

                            form2.label2.Text = Convert.ToString(C);
                            form2.label5.Text = Convert.ToString(N);
                            form2.label8.Text = TypeS;
                            if (checkBox2.Checked == true)
                            {
                                form2.button1.Click += (senderSlave, eSlave) =>
                                {
                                    SoglasieNaDopRaschet = true;
                                };
                                form2.label7.Text = " Старые приводы. Рекомендуем доп.группу. Unmr = " + Unmr;
                                form2.button1.Show();
                                form2.ShowDialog();
                                if (SoglasieNaDopRaschet == true)    //доп расчет копируется с первого доп расчета
                                {
                                    string[] ArrayOfCharacter = new string[4];
                                    ArrayOfCharacter = DopGroup(Convert.ToInt32(Iprod), Convert.ToInt32(Ikratk), Convert.ToInt32(Tprod), Convert.ToInt32(Tkratk), U,TableName1);
                                    form2.label2.Text = ArrayOfCharacter[0];
                                    form2.label5.Text = ArrayOfCharacter[1];
                                    form2.label8.Text = ArrayOfCharacter[2];
                                    form2.label7.Text = ArrayOfCharacter[3];
                                    form2.button1.Hide();
                                    form2.ShowDialog();
                                }
                            }
                            else
                            {
                                form2.label7.Text = "  Рекомендаций нет. Unmr = " + Unmr;
                                form2.button1.Hide();
                            }
                            form2.ShowDialog();
                        }
                    }


                    else
                    {
                        N = Convert.ToInt32(Math.Round(1.1 * U / 2.23));
                        Uz = 1.1 * U / N;
                        Unmr = (0.85 * U + dU) / N;
                        D = false;
                        C = GetCapacity(Convert.ToInt32(Iprod), Convert.ToInt32(Ikratk), Convert.ToInt32(Tprod), Convert.ToInt32(Tkratk), Unmr, D,TableName1);
                        TypeS = GetType(C,TableName1);
                        if (Unmr > 1.9)
                        {

                            form2.label2.Text = Convert.ToString(C);
                            form2.label5.Text = Convert.ToString(N);
                            form2.label8.Text = TypeS;
                            form2.label7.Text = "  Установка устройства стабилизации не позволяет обеспечить напряжение на аккумуляторах"
                              + "\nв конце разряда ниже 1,9 В, что может иметь место на реконструируемых подстанциях"
                              + "\nс выключателями, оборудованными электромагнитными приводами прямого действия."
                              + "\nРекомендуется рассмотреть возможность организации дополнительной группы аккумуляторов"
                              + "\nв батарее для компенсации потери напряжения в цепи питания кратковременной нагрузки. Unmr = " + Unmr;
                            form2.ShowDialog();


                        }
                        else
                        {
                            form2.label2.Text = Convert.ToString(C);
                            form2.label5.Text = Convert.ToString(N);
                            form2.label8.Text = TypeS;
                            form2.label7.Text = "  Установка устройства стабилизации обеспечивает напряжение на аккумуляторах"
                              + "\nв конце разряда ниже 1,9 В. Рекомендаций нет.Unmr = " + Unmr;
                            form2.button1.Hide();
                            form2.ShowDialog();
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (string.IsNullOrEmpty(maskedTextBox5.Text))
                {
                    label10.Text = "Это поле является обязательным!";
                    label10.Show(); }
                else
                {
                  if (Convert.ToInt32(maskedTextBox5.Text) == 0)
                  { label10.Text = "Значение должно быть больше 0!";
                    label10.Show(); }
                }
                if (string.IsNullOrEmpty(maskedTextBox1.Text))
                { label2.Show(); }
                if (string.IsNullOrEmpty(maskedTextBox2.Text))
                { label8.Show(); }
                if (string.IsNullOrEmpty(maskedTextBox3.Text))
                { label7.Show(); }
                if (string.IsNullOrEmpty(maskedTextBox4.Text))
                { label9.Show(); }
                if (string.IsNullOrEmpty(maskedTextBox6.Text)|| string.IsNullOrEmpty(maskedTextBox7.Text))
                { label22.Show(); }
                if (string.IsNullOrEmpty(maskedTextBox13.Text) || string.IsNullOrEmpty(maskedTextBox12.Text))
                { label24.Show(); }



            }
        }

    
        

    private void textBox1_TextChanged(object sender, EventArgs e)
        {

            
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        public void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        public void textBox5_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void maskedTextBox5_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
          
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string NameOfTable = Convert.ToString(comboBox1.SelectedItem);

            Form2A form2A = new Form2A();
            form2A.Owner = this;
            form2A.Hide();
                                

            if (NameOfTable == "Ввести новый")
            {
                form2A.ShowDialog();
            }
            else
            {
                MessageBox.Show("Невозможно изменить данную таблицу. \nДля ввода другой таблицы выберете 'Ввести новый'.");
            }
        }

        private void deleteToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (Convert.ToString(comboBox1.SelectedItem) == "Ввести новый" || Convert.ToString(comboBox1.SelectedItem) == "GroE")
            {
                MessageBox.Show("Невозможно удалить");
            }
            else
            {
                if (MessageBox.Show("Вы действительно хотите удалить таблицу?", "Удалить",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string TableName1 = Convert.ToString(comboBox1.SelectedItem);
                    string TableName2 = "CCD_" + Convert.ToString(comboBox1.SelectedItem);
                    string connectionString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + System.IO.Path.Combine(Application.StartupPath, "ClassicGROE.mdb");
                    OleDbConnection myOleDbConnection = new OleDbConnection(connectionString);
                    myOleDbConnection.Open();
                    OleDbCommand DeleteTable = myOleDbConnection.CreateCommand();
                    DeleteTable.CommandText = string.Format("DROP TABLE IF EXISTS " + TableName1+","+ TableName2, myOleDbConnection);
                    OleDbDataReader myOleDbDataReader2 = DeleteTable.ExecuteReader();
                    myOleDbDataReader2.Read();
                    myOleDbDataReader2.Close();
                    myOleDbConnection.Close();
                    comboBox1.Items.Remove(TableName1);
                }
            }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked==true)
            {
                label12.Enabled = true;
                label13.Enabled = true;
                label14.Enabled = true;
                label15.Enabled = true;
                label16.Enabled = true;
                label17.Enabled = true;
                label18.Enabled = true;
                label19.Enabled = true;
                label20.Enabled = true;
                label21.Enabled = true;
                maskedTextBox6.Enabled = true;
                maskedTextBox7.Enabled = true;
                maskedTextBox8.Enabled = true;
                maskedTextBox9.Enabled = true;
                maskedTextBox10.Enabled = true;
                maskedTextBox11.Enabled = true;
                maskedTextBox12.Enabled = true;
                maskedTextBox13.Enabled = true;

            }
            else
            {
                label12.Enabled = false;
                label13.Enabled = false;
                label14.Enabled = false;
                label15.Enabled = false;
                label16.Enabled = false;
                label17.Enabled = false;
                label18.Enabled = false;
                label19.Enabled = false;
                label20.Enabled = false;
                label21.Enabled = false;
                maskedTextBox6.Enabled = false;
                maskedTextBox7.Enabled = false;
                maskedTextBox8.Enabled = false;
                maskedTextBox9.Enabled = false;
                maskedTextBox10.Enabled = false;
                maskedTextBox11.Enabled = false;
                maskedTextBox12.Enabled = false;
                maskedTextBox13.Enabled = false;
            }
        }
        //private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        //{

        //   }
    }
}
