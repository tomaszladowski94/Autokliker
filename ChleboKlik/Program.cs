using System;
using System.Runtime.InteropServices; //do importowania dllek i odczytywania przyciskow z wybranych miejsc z pamieci
using System.Diagnostics; //do zbudowania hooka
using System.Windows.Forms; //zeby odpalic apke
using System.Threading;
using System.Drawing;

namespace ChleboKlik
{
    class Program
    {
        private static int WH_KEYBOARD_LL = 13; //zmienna do definiowania typu hooka 13 - hook dla niskopoziomowych zdarzen wejsciowych
        private static int WM_KEYDOWN = 0x0100; //zmienna do porownywania w funkcji HookCallback, kiedy zdarzy sie wejscie jesli wParam bedzie rown wm_keydown to ze nie systemowe gowno wpisal user
        private static IntPtr hook = IntPtr.Zero; //adres procedury hookujacej czyli wh_keyboard_ll
        private static LowLevelKeyboardProc llkp = HookCallback; //delegat dla hookcallback ktort definiuje co chcemy zeby sie dzialo po nacisnieciu guwna

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private static int spaceCount = 0;

        private static Thread chlebek = new Thread(() => klikaj_myszon()) {IsBackground = true};

        private static EventWaitHandle ewh;

        private static Random random = new Random();

        private static bool odpalony = false;

        private static Point myszka;// = new Point(Cursor.Position.X,Cursor.Position.Y);

        private static int ilosc_klikniec = 0;
        private static int kliknieto = 0;

        private static int[] cooldowny;
        static void Main(string[] args)
        {
            pytanko();
            ewh = new ManualResetEvent(initialState: false);
            hook = SetHook(llkp);
            chlebek.Start();
            Application.Run(); //cos jak nieskonczona petla zeby se ciagle dzialalo w tle
            UnhookWindowsHookEx(hook); //do wylaczania hooka trzeba dllki 
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //nCOde kod procedury hookowej jak mniejszy od zera to trzeba przekazac wiadomosc do callnexthookex funkcji bez dalszego przetwarzania i zwroci wartosc z callnexthookex
            //wparam identyfikator klawiaturowej wiadomosci wm_keydown, wm_keyup, wm_syskeydown, wm_syskeyup
            if(nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                bool CapsLock = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;
                //vkcode virtual key code Marshal.readint32(costam) - int wartosci adresu pamieci lparam odpowiada keyowi nacisnietemu
                int vkCode = Marshal.ReadInt32(lParam);
                char c = (char)vkCode;
                string s;
                Keys k;
                if (vkCode >= 65 && vkCode <= 90)
                {
                    if (!CapsLock)
                        s = c.ToString().ToLower();
                    else
                        s = c.ToString().ToUpper();
                }
                else
                {
                    k = (Keys)vkCode;
                    if (k == Keys.Space)
                        spaceCount++;
                    s = k.ToString();
                }
                Console.Out.Write(s); //windows forms keys enumeration zmienia inta w odczytywalny format
                if(spaceCount%3==0 && spaceCount!=0 && !odpalony)
                {
                    myszka = new Point(Cursor.Position.X, Cursor.Position.Y);
                    odpalony = true;
                    ewh.Set();
                    AutoClosingMessageBox.Show("Bułkoklikator odpalony",":--DDD",1000);
                }
                if(spaceCount%5==0 && spaceCount!=0 && odpalony)
                {
                    spaceCount = 0;
                    odpalony = false;
                    ewh.Reset();
                    AutoClosingMessageBox.Show("Bułkoklikator nieaktywny jak twój penis :---DDD", ":--DDD", 1000);
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)//funkcja budujaca setwindowsex
        {
            Process currProc = Process.GetCurrentProcess();
            ProcessModule currModule = currProc.MainModule;
            String moduleName = currModule.ModuleName;
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            return SetWindowsHookEx(WH_KEYBOARD_LL, llkp, moduleHandle, 0);
        }
        private static void DoMouseClick()
        {
            //Call the imported function with the cursor's current position
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
            Cursor.Position = new Point(myszka.X - random.Next(0, 2), myszka.Y - random.Next(0, 2));
            Cursor.Position = new Point(myszka.X - random.Next(0, 2), myszka.Y - random.Next(0, 2));
            Cursor.Position = new Point(myszka.X - random.Next(0, 2), myszka.Y - random.Next(0, 2));
            Cursor.Position = new Point(myszka.X - random.Next(0, 2), myszka.Y - random.Next(0, 2));
        }

        private static void klikaj_myszon()
        {
            while (true)
            {
                ewh.WaitOne();
                DoMouseClick();
                Thread.Sleep(random.Next(1500,2500));
                kliknieto++;
                for(int i=0;i<cooldowny.Length;i++)
                {
                    if(kliknieto==cooldowny[i])
                    {
                        int sleep = random.Next(20000, 50000);
                        Console.WriteLine("Cooldown na " + kliknieto + " kliku, potrwa " + sleep + "ms");
                        Thread.Sleep(sleep);
                    }
                }
                if(kliknieto==ilosc_klikniec)
                {
                    spaceCount = 0;
                    odpalony = false;
                    ewh.Reset();
                    AutoClosingMessageBox.Show("Bułkoklikator nieaktywny jak twój penis :---DDD", ":--DDD", 1000);
                    pytanko();
                }
            }
        }

        private static void pytanko()
        {
            Console.WriteLine("PODAJ ILOSC ALCHUW");
            bool mamy_inta = false;
            do
            {
                string dupa = Console.ReadLine();
                if (!string.IsNullOrEmpty(dupa))
                {
                    int tryInt;
                    if (Int32.TryParse(dupa, out tryInt))
                    {
                        Console.WriteLine("MAMY INTA: " + tryInt);
                        mamy_inta = true;
                        ilosc_klikniec = tryInt * 2;
                    }

                }
            }
            while (!mamy_inta);

            if (ilosc_klikniec > 0)
            {
                Console.WriteLine("Cooldowny co:");
                cooldowny = new int[random.Next(5,15)];
                int suma = 0;
                int poczatek = 0;
                int reszta = ilosc_klikniec / cooldowny.Length*2;
                for (int i = 0; i < cooldowny.Length-1; i++)
                {
                    if(i==0)
                        cooldowny[i] = random.Next(poczatek, reszta);
                    else
                        cooldowny[i]=cooldowny[i-1] + random.Next(poczatek, reszta);
                    //poczatek = cooldowny[i];
                    //reszta += cooldowny[i];
                    suma += cooldowny[i];
                    Console.Write(cooldowny[i] + ", ");
                }
                cooldowny[cooldowny.Length - 1] = ilosc_klikniec;
                Console.WriteLine(cooldowny[cooldowny.Length - 1]);
            }
        }

        //funkcje winapi
        [DllImport("user32.dll")] //ma callnexthookex
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc llkp, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(String modulename);
    }
}
