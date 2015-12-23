using System.Collections.Generic;
using System.IO;
using System;

class FileIO
{
    string[] loggingString;
    int linesWritten;
    int lastLineSaved;
    int columns;
    string filePath;
    string fileName;
    int linesPerCommit;
    StreamWriter writer;
    bool dirty;
    bool logging;
    LoggingButtonHandler handler;

    Queue<dataCorrection> dataProcessingCorrections;

    public FileIO(int columns, string filepath, string filename, int linesPerCommit, LoggingButtonHandler handler)
    {
        construct(columns, filepath, filename, linesPerCommit, handler);
    }
    public FileIO(int columns, string filepath, string filename, int linesPerCommit, string[] headers, LoggingButtonHandler handler)
    {
        construct(columns, filepath, filename, linesPerCommit, handler);
        int i = 0;
        foreach (string head in headers)
        {
            setColumn(i, head);
            i++;
        }
        logging = true;
        Log();
        logging = false;
    }
    private void construct(int columns, string filepath, string filename, int linesPerCommit, LoggingButtonHandler handler)
    {

        if (!System.IO.Directory.Exists(filepath))
        {
            System.IO.Directory.CreateDirectory(filepath);
        }
        if (!System.IO.Directory.Exists(filepath+ @"Templates\Load"))
        {
            System.IO.Directory.CreateDirectory(filepath + @"Templates\Load");
        }

        loggingString = new string[columns];
        linesWritten = 0;
        this.columns = columns;
        SetFileName(filename);
        SetFilePath(filepath);
        this.handler = handler;

        writer = new StreamWriter(filePath+fileName);
        handler.SetDefaultPaths(filePath, fileName);
        this.linesPerCommit = linesPerCommit;
        dirty = false;
        logging = false;

        for(int i = 0; i < columns; i++)
        {
            loggingString[i] = string.Empty;
        }

        dataProcessingCorrections = new Queue<dataCorrection>();
    }
    public bool Log()
    {
        if (logging)
        {
            if(dirty)
            {
                string psring = string.Empty;
                for (int i = 0; i < columns; i++)
                {
                    psring += loggingString[i] + "\t";
                }
                writer.WriteLine(psring);
                linesWritten++;
                if (linesWritten % linesPerCommit == 0)
                {
                    writer.Flush();
                }
                flush();
                dirty = false;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    public void exitLog()
    {
        if(dataProcessingCorrections.Count > 0)
        {
            StreamReader processingReader = new StreamReader(filePath + fileName);
            string processedFileName = fileName.Insert(fileName.Length - 4, "_processed");
            StreamWriter processingWriter = new StreamWriter(filePath + processedFileName);

            int currentLine = lastLineSaved;
            dataCorrection correction = dataProcessingCorrections.Dequeue();
            while (correction != null && currentLine < linesWritten)
            {
                string line = processingReader.ReadLine();
                if(currentLine == correction.getLine())
                {
                    bool sameLine = true;
                    string[] columns = line.Split('\t');
                    string prior = columns[0];
                    int column = 1;
                    while (sameLine)
                    {
                        if (correction.getColumn() == 0)
                        {
                            columns[0] = correction.getData();
                        }
                        else
                        {
                                for (int index = column;  column < columns.Length; column++)
                                {
                                    if(index == correction.getColumn())
                                    {
                                        columns[column] = correction.getData();
                                    break;
                                    }
                                    else if (!prior.Equals('\t') && !columns[index].Equals('\t'))
                                    {
                                        prior = columns[index];
                                        index++;
                                    }
                                }
                        }
                        correction = dataProcessingCorrections.Dequeue();
                        if (correction.getLine() != currentLine)
                        {
                            sameLine = false;
                        }
                    }
                }
                currentLine++;
            }
            if(correction!=null)
            {
                throw new IndexOutOfRangeException("Processing correction still pending after end of file!");
            }
            lastLineSaved = currentLine;
        }
    }
    public void setColumn(int column, string data)
    {
        if (!dirty)
        {
            dirty = true;
            loggingString[column] = data;
        }
        else if (!loggingString[column].Equals(string.Empty) && !loggingString[column].Equals("\t"))
        {
            Log();
        }
        loggingString[column] = data;
    }

    private void flush()
    {
        for (int i = 0; i < columns; i++)
        {
            loggingString[i] = string.Empty;
        }
    }
    public void close()
    {
        Log();
        exitLog();
        writer.Flush();
        writer.Close();
    }
    public bool toggleLogging()
    {
        logging = !logging;
        if(!logging)
        {
            exitLog();
        }
        return logging;
    }
    public void SetFilePath(string path)
    {
        filePath = path;
    }
    public void SetFileName(string name)
    {
        fileName = name;
    }
    public void pushCorrections(int columnNumber, string[] newData)
    {
        int lines = newData.Length;
        for (int i = 0; i < lines; i++)
        {
            dataProcessingCorrections.Enqueue(new dataCorrection(linesWritten - lines + i, columnNumber, newData[i]));
        }
    }
    public void pushCorrections(dataCorrection correction)
    {
        dataProcessingCorrections.Enqueue(correction);
    }
    public void pushCorrections(Queue<dataCorrection> corrections)
    {
        while(corrections.Count>0)
        {
            dataProcessingCorrections.Enqueue(corrections.Dequeue());
        }
    }
    public interface LoggingButtonHandler
    {
        void LoggingButtonPress();
        void SetDefaultPaths(string filePath, string fileName);
    }
    public int getLine()
    {
        return linesWritten;
    }
    public string getData(int column)
    {
        return loggingString[column];
    }
    public class dataCorrection
    {
        int line;
        int column;
        string data;

        public dataCorrection(int lineNumber, int columnNumber, string newData)
        {
            line = lineNumber;
            column = columnNumber;
            data = newData;
        }
        public int getLine()
        {
            return line;
        }
        public int getColumn()
        {
            return column;
        }
        public string getData()
        {
            return data;
        }
    }

    public Queue<string[]> ImportGrid()
    {
        string path = filePath + @"\Templates\Load";

        string[] fileNames = Directory.GetFiles(path);


        System.IO.FileStream filestream = new System.IO.FileStream(fileNames[0],
                                          System.IO.FileMode.Open,
                                          System.IO.FileAccess.Read,
                                          System.IO.FileShare.Read);
        System.IO.StreamReader file = new System.IO.StreamReader(filestream);

        string data;
        Queue<string[]> vectors = new Queue<string[]>();
        while ((data = file.ReadLine()) != null)
        {
            vectors.Enqueue(data.Split('\t'));
        }
        file.Close();
        return vectors;
    }
}
