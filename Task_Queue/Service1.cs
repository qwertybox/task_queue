using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.Timers;
//using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Task_Queue
{
    public partial class Service1 : ServiceBase
    {
        public static List<string> Queue;
        public static int f = 0;
        public static int id_element = 0;
        public static int Quantity = 1;
        public static double Task_Execution_Duration = 60000;
        public static int percent = 3;       
        public static int percentcur = 0;
        public static double Task_claim_check=30000;
        public Service1() 
        {
            InitializeComponent();
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Task_Queue\Parameters");

            //if it does exist, retrieve the stored values  
            if (key != null)
            {
                string Task_Claim_Check_Period = (string)key.GetValue("Task_Claim_Check_Period", "30000");
                string Task_Execution = (string)key.GetValue("Task_Execution_Duration", "60000");
                string Quantity_str = (string)key.GetValue("Quantity", "1");
                key.Close();
                double g = Convert.ToDouble(Task_Claim_Check_Period);
                Task_claim_check = g;
                double s = Convert.ToDouble(Task_Execution);
                Task_Execution_Duration = s;
                int quant = Convert.ToInt32(Quantity_str);
                Quantity = quant;

            }
            int percentage = Convert.ToInt32(Task_Execution_Duration)/1000;
            percent = 200/percentage;
        }

        protected override void OnStart(string[] args)
        {            
            Timer t = new Timer();
            t.Interval = Task_claim_check;

            t.Elapsed += new ElapsedEventHandler(WorkingCycle); // запускает первый цикл в первом потоке
            t.Enabled = true;

            System.Threading.Thread TH = new System.Threading.Thread(ThreadWorkingCycle); // запускает второй поток
            TH.Start();
        }

        private static void WorkingCycle(object source, ElapsedEventArgs e) //первый цикл, проверяет было ли что-то изменено в директории claims и создает задание в Tasks
        {
            string[] folders = Directory.GetDirectories(@"C:\Mine\Task_queue\Claims", "*" , SearchOption.AllDirectories);
            List<int> folderids = new List<int>();
            foreach (string i in folders)
            {
                folderids.Add(Convert.ToInt32(i.Substring(31)));                   
            }
            string Claimed = ClaimsMethod(folderids, folders);
            if (Check_method(Claimed.Substring(26)))
            {
                if (Directory.Exists(Claimed))
                {
                    Directory.Delete(Claimed);
                }
                string NewTask = @"C:\Mine\Task_queue\Tasks\" + Claimed.Substring(26) + "-" + "[....................]-" + "Queued";
                if (!Directory.Exists(NewTask))
                {
                    Directory.CreateDirectory(NewTask);
                    WriteLog(Claimed.Substring(26, 9),1);
                }
                else
                {
                    WriteLog(Claimed.Substring(26), 3);
                    Directory.Delete(Claimed);
                }

            }
            else
            {
                WriteLog(Claimed.Substring(26), 2);
                if (Directory.Exists(Claimed))
                {
                    Directory.Delete(Claimed);
                }
            }
            
        }

        private static string ClaimsMethod(List<int> folderids, string[] folders)
        {
            int minid = folderids.Min();
            foreach (string i in folders)
            {
                if (i.Contains(minid.ToString()))
                {
                    return i;
                }
            }
            return "";
        }

        private static void ThreadWorkingCycle() // второй поток
        {
            Timer t2 = new Timer();
            t2.Interval = (double)Task_Execution_Duration;
            t2.Elapsed += new ElapsedEventHandler(WorkingCycle2);
            t2.Enabled = true;
        }

        protected override void OnStop()
        {
            //WriteLog("stopped", 5);
        }

        private static bool Check_method(string Name)
        {
            Regex R = new Regex(@"^Task_\d{4}$");
            Match M = R.Match(Name);
            return M.Success;
        }

        private static void WriteLog(string z, int id)
        {
            using (StreamWriter F = new StreamWriter("C:\\Mine\\Task_queue\\logs\\TaskQueue_" + DateTime.Today.ToShortDateString(), true))
            {
                F.WriteLine("-----------------------------------------<" + DateTime.Now + ">----------------------------------------------------------------");
                if (id == 1)
                {
                    F.WriteLine("Задача " + z + " успішно прийнята в обробку...");
                } else if (id == 2)
                {
                    F.WriteLine("Помилка розміщення заявки "+z+". Некоректний синтаксис ...");
                }
                else if (id == 3)
                {
                    F.WriteLine("Помилка розміщення заявки " + z + ". Номер вже існує ...");
                }
                else
                {
                    F.WriteLine("Задача "+ z + " успішно ВИКОНАНА!");
                }
            }
        }

        private static void WorkingCycle2(object source, ElapsedEventArgs e) // add this new to another directory and delete it from old one.
        {
            Queue = new List<string>();
            string[] folders = Directory.GetDirectories(@"C:\Mine\Task_queue\Tasks", "*", SearchOption.AllDirectories);
            if (Quantity == 1)
            {
                for (int i = 0; i < folders.Length; i++)
                {
                    Queue.Add(folders[i]);
                }

                Timer t3 = new Timer();
                t3.Interval = 2000;
                t3.Elapsed += new ElapsedEventHandler(RenameMethod1);
                t3.Enabled = true;
            }
            else if ((Quantity == 2)&&(folders.Length>=2))
            {
                for (int i = 0; i < folders.Length; i++)
                {
                    Queue.Add(folders[i]);
                }
                Timer t3 = new Timer();
                t3.Interval = 2000;
                t3.Elapsed += new ElapsedEventHandler(RenameMethod2);
                t3.Enabled = true;
            }
            else if ((Quantity == 3) && (folders.Length >= 3))
            {
                for (int i = 0; i < folders.Length; i++)
                {
                    Queue.Add(folders[i]);
                }
                Timer t3 = new Timer();
                t3.Interval = 2000;
                t3.Elapsed += new ElapsedEventHandler(RenameMethod3);
                t3.Enabled = true;
            }
        }
        private static void RenameMethod1(object source, ElapsedEventArgs e)
        {
            percentcur = percentcur + percent;
            string progressbar = "[....................]";
            string pathfolder = "";
            if (Queue[id_element].Contains("completed"))
            {
                id_element++;
            }
            else
            {
                if ((percentcur >= 25) && (percentcur < 50))
                {
                    pathfolder = Queue[id_element];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    progressbar = "[IIIII...............]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 50) && (percentcur < 75))
                {
                    pathfolder = Queue[id_element];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    progressbar = "[IIIIIIIIII..........]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 75) && (percentcur < 100))
                {
                    pathfolder = Queue[id_element];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    progressbar = "[IIIIIIIIIIIIIII.....]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 100))
                {
                    pathfolder = Queue[id_element];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    progressbar = "[IIIIIIIIIIIIIIIIIIII]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "completed");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "completed";
                    WriteLog(pathfolder.Substring(25, 9), 4);
                    percentcur = 0;
                    progressbar = "[....................]";
                }
                else if (percentcur < 25)
                {
                    pathfolder = Queue[id_element];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
            }
        }
        private static void RenameMethod2(object source, ElapsedEventArgs e)
        {
            percentcur = percentcur + percent;
            string progressbar = "[....................]";
            string pathfolder = "";
            string pathfolder1 = "";
            if (Queue[id_element].Contains("completed"))
            {
                id_element++;
            }
            else
            {
                if ((percentcur >= 25) && (percentcur < 50))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    progressbar = "[IIIII...............]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element+1] = Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 50) && (percentcur < 75))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    progressbar = "[IIIIIIIIII..........]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element+1] = Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 75) && (percentcur < 100))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    progressbar = "[IIIIIIIIIIIIIII.....]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                    
                    Directory.Move(pathfolder1, Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element+1] = Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 100))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    progressbar = "[IIIIIIIIIIIIIIIIIIII]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "completed");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "completed";
                    
                    Directory.Move(pathfolder1, Queue[id_element+1] + "-" + progressbar + "-" + "completed");
                    Queue[id_element+1] = Queue[id_element+1] + "-" + progressbar + "-" + "completed";
                    WriteLog(Queue[id_element].Substring(25, 9), 4);
                    WriteLog(Queue[id_element+1].Substring(25, 9), 4);
                    percentcur = 0;
                    progressbar = "[....................]";
                    id_element++;
                    id_element++;
                }
                else if (percentcur < 25)
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element+1] = Queue[id_element+1].Substring(0, 35);
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element+1] = Queue[id_element+1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
            }
        }
        private static void RenameMethod3(object source, ElapsedEventArgs e)
        {
            percentcur = percentcur + percent;
            string progressbar = "[....................]";
            string pathfolder = "";
            string pathfolder1 = "";
            string pathfolder2 = "";
            if (Queue[id_element].Contains("completed"))
            {
                id_element++;
                id_element++;
            }
            else
            {
                if ((percentcur >= 25) && (percentcur < 50))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    pathfolder2 = Queue[id_element + 2];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    Queue[id_element + 2] = Queue[id_element + 2].Substring(0, 35);
                    Queue[id_element + 2] = Queue[id_element + 2].Substring(0, 35);
                    progressbar = "[IIIII...............]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 1] = Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder2, Queue[id_element + 2] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 2] = Queue[id_element + 2] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 50) && (percentcur < 75))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    pathfolder2 = Queue[id_element + 2];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    Queue[id_element + 2] = Queue[id_element + 2].Substring(0, 35);
                    progressbar = "[IIIIIIIIII..........]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 1] = Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 75) && (percentcur < 100))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    pathfolder2 = Queue[id_element + 2];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    Queue[id_element + 2] = Queue[id_element + 2].Substring(0, 35);
                    progressbar = "[IIIIIIIIIIIIIII.....]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 1] = Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder2, Queue[id_element + 2] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 2] = Queue[id_element + 2] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
                else if ((percentcur >= 100))
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    pathfolder2 = Queue[id_element + 2];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    Queue[id_element + 2] = Queue[id_element + 2].Substring(0, 35);
                    progressbar = "[IIIIIIIIIIIIIIIIIIII]";
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "completed");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "completed";

                    Directory.Move(pathfolder1, Queue[id_element + 1] + "-" + progressbar + "-" + "completed");
                    Queue[id_element + 1] = Queue[id_element + 1] + "-" + progressbar + "-" + "completed";

                    Directory.Move(pathfolder2, Queue[id_element + 2] + "-" + progressbar + "-" + "completed");
                    Queue[id_element + 2] = Queue[id_element + 2] + "-" + progressbar + "-" + "completed";
                    WriteLog(Queue[id_element].Substring(25, 9), 4);
                    WriteLog(Queue[id_element+1].Substring(25, 9), 4);
                    WriteLog(Queue[id_element+2].Substring(25, 9), 4);
                    percentcur = 0;
                    progressbar = "[....................]";
                    id_element++;
                    id_element++;
                    id_element++;
                }
                else if (percentcur < 25)
                {
                    pathfolder = Queue[id_element];
                    pathfolder1 = Queue[id_element + 1];
                    pathfolder2 = Queue[id_element + 2];
                    Queue[id_element] = Queue[id_element].Substring(0, 35);
                    Queue[id_element + 1] = Queue[id_element + 1].Substring(0, 35);
                    Queue[id_element + 2] = Queue[id_element + 2].Substring(0, 35);
                    Directory.Move(pathfolder, Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element] = Queue[id_element] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder1, Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 1] = Queue[id_element + 1] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";

                    Directory.Move(pathfolder2, Queue[id_element + 2] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents");
                    Queue[id_element + 2] = Queue[id_element + 2] + "-" + progressbar + "-" + "In progress-" + percentcur + "-" + "percents";
                }
            }

        }

        private static string Progress(int id)
        {
            return "";
        }
    }
}
