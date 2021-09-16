using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Archiver
{
    class HuffmanTree
    {
        public char ch { get; private set; }
        public double freq { get; private set; }
        public bool isTerminal { get; private set; }
        public HuffmanTree left {get; private set;}
        public HuffmanTree rigth {get; private set;}
        public HuffmanTree(char c, double frequency)
        {
            ch = c;
            freq = frequency;
            isTerminal = true;
            left = rigth = null;
        }
        public HuffmanTree(HuffmanTree l, HuffmanTree r)
        {
            freq = l.freq + r.freq;
            isTerminal = false;
            left = l;
            rigth = r;
        }
    }
    class HuffmanInfo
    {
        List<HuffmanTree> treeNodes = new List<HuffmanTree>();
		HuffmanTree Tree; // дерево кода Хаффмана, потребуется для распаковки
        Dictionary<char,string> Table; // словарь, хранящий коды всех символов, будет удобен для сжатия
        public HuffmanInfo(string fileName)
        {   
			string line;		
            StreamReader sr = new StreamReader(fileName, Encoding.Unicode);
            // считать информацию о частотах символов
			while ((line = sr.ReadLine()) != null)
			{
				if (line.Length == 0)
				{
                    //TODO: отдельная обработка строки, которой соответствует частота символа "конец строки" 
                    line = sr.ReadLine().Trim();
                    double freq = Convert.ToDouble(line);
                    treeNodes.Add(new HuffmanTree('\n', freq));
				}
				else
				{
                    //TODO: создаем вершину (лист) дерева с частотой очередного символа
                    char ch = line[0];
                    double freq = Convert.ToDouble(line.Substring(2));
                    treeNodes.Add(new HuffmanTree(ch, freq));
				}
			}
            sr.Close();
            // TODO: добавить еще одну вершину-лист, соответствующую символу с кодом 0 ('\0'), который будет означать конец файла. Частота такого символа, очевидно, должна быть очень маленькой, т.к. такой символ встречается только 1 раз во всем файле (можно даже сделать частоту = 0)
            treeNodes.Add(new HuffmanTree('\0', 0));
            // TODO: построить дерево кода Хаффмана путем последовательного объединения листьев
            int minNode1 = 0, minNode2 = 0;
            while (treeNodes.Count > 1) {
                FindMinNodesInTree(treeNodes, ref minNode1, ref minNode2);
                HuffmanTree newNode = new HuffmanTree(treeNodes[minNode1], treeNodes[minNode2]);
                treeNodes.RemoveAt(Math.Max(minNode1,minNode2));
                treeNodes.RemoveAt(Math.Min(minNode1,minNode2));
                treeNodes.Add(newNode);
            }
            Tree = treeNodes[0];
            Table = new Dictionary<char, string>();
            // TODO: заполнить таблицу кодирования Table на основе обхода построенного дерева
            FillTable(Tree, "");
            //Table.Add('\0', "0"); // это временная заглушка!!! Эту строчку нужно будет потом убрать, т.к. признак конца файла должен быть уже добавлен в таблицу, как и все остальные символы
        }

        private void FindMinNodesInTree(List<HuffmanTree> tree,ref int minNode1,ref int minNode2) {
            if (tree[0].freq < tree[1].freq) {
                minNode1 = 0;
                minNode2 = 1;
            }
            else {
                minNode1 = 1;
                minNode2 = 0;
            }

            for(int i = 2; i < tree.Count(); i++) {
                if (tree[i].freq < tree[minNode1].freq) {
                    int temp = minNode1;
                    minNode1 = i;

                    if (tree[temp].freq < tree[minNode2].freq)
                        minNode2 = temp;
                }
                else if (tree[i].freq < tree[minNode2].freq)
                    minNode2 = i;
            }
        }

        private void FillTable(HuffmanTree node, string code) {
            if (node.isTerminal) {
                Table[node.ch] = code;
            }
            else {
                FillTable(node.left, code + "0");
                FillTable(node.rigth, code + "1");
            }
        }

        public void Compress(string inpFile, string outFile)
        {
            var sr = new StreamReader(inpFile, Encoding.Unicode);
            var sw = new ArchWriter(outFile); //нужна побитовая запись, поэтому использовать StreamWriter напрямую нельзя
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                // TODO: посимвольно обрабатываем строку, кодируем, пишем в sw
                foreach(char symb in line) {
                    sw.WriteWord(Table[symb]);
                }
                sw.WriteWord(Table['\n']);
            }
            sr.Close();
            sw.WriteWord(Table['\0']); // записываем признак конца файла
            sw.Finish();
        }
        public void Decompress(string archFile, string txtFile)
        {
            var sr = new ArchReader(archFile); // нужно побитовое чтение
            var sw = new StreamWriter(txtFile, false, Encoding.Unicode);
            byte curBit;
            HuffmanTree nodeTree = Tree;
            while (sr.ReadBit(out curBit))
            {
                // TODO: побитово (!) разбираем архив
                if (nodeTree.isTerminal) {
                    char symb = nodeTree.ch;
                    if (symb == '\0') break;
                    else {
                        sw.Write(symb);
                        nodeTree = Tree;
                    }
                }
                if (curBit == 0) nodeTree = nodeTree.left;
                else if (curBit == 1) nodeTree = nodeTree.rigth;
            }
            sr.Finish();
            sw.Close();
        }
    }

    class Huffman
    {
        static void Main(string[] args)
        {
            if (!File.Exists("freq.txt"))
            {
                Console.WriteLine("Не найден файл с частотами символов!");
                return;
            }
            if (args.Length == 0)
            {
                var hi = new HuffmanInfo("freq.txt");
                hi.Compress("etalon2.txt", "etalon2.arc");
                hi.Decompress("etalon2.arc", "etalon_dec2.txt");
                return;
            }
            if (args.Length != 3 || args[0] != "zip" && args[0] != "unzip")
            {
                Console.WriteLine("Синтаксис:");
                Console.WriteLine("Huffman.exe zip <имя исходного файла> <имя файла для архива>");
                Console.WriteLine("либо");
                Console.WriteLine("Huffman.exe unzip <имя файла с архивом> <имя файла для текста>");
                Console.WriteLine("Пример:");
                Console.WriteLine("Huffman.exe zip text.txt text.arc");
                return;
            }
            var HI = new HuffmanInfo("freq.txt");
            if (args[0] == "zip")
            {
                if (!File.Exists(args[1]))
                {
                    Console.WriteLine("Не найден файл с исходным текстом!");
                    return;
                }
                HI.Compress(args[1], args[2]);
            }
            else
            {
                if (!File.Exists(args[1]))
                {
                    Console.WriteLine("Не найден файл с архивом!");
                    return;
                }
                HI.Decompress(args[1], args[2]);
            }
            Console.WriteLine("Операция успешно завершена!");
        }
    }
}
