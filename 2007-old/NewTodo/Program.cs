using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace NewTodo {
    static class Program {
        static void Main(string[] args) {
            DirectoryInfo curdir = new FileInfo(Application.ExecutablePath).Directory;
            string basenewname = "Todo "+DateTime.Today.ToString("yyyy-MM-dd")+".txt";
            FileInfo newfile = new FileInfo(Path.Combine(curdir.FullName,basenewname));
            if(!newfile.Exists)newfile.CreateText().Close();
            System.Diagnostics.Process.Start(newfile.FullName);
        }
    }
}