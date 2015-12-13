using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Logger
{
    string[] loggingString;
    int linesWritten;
    int columns;
    string filePath;
    int linesPerCommit;
    System.IO.StreamWriter writer;
    bool dirty;

    public Logger(int columns, string filepath, string filename, int linesPerCommit)
    {
        construct(columns, filepath, filename, linesPerCommit);
    }
    public Logger(int columns, string filepath, string filename, int linesPerCommit, string[] headers)
    {
        construct(columns, filepath, filename, linesPerCommit);
        int i = 0;
        foreach (string head in headers)
        {
            setColumn(i, head);
            i++;
        }
        Log();
    }
    private void construct(int columns, string filepath, string filename, int linesPerCommit)
    {
        loggingString = new string[columns];
        linesWritten = 0;
        this.columns = columns;
        this.filePath = filepath + filename;

        writer = new System.IO.StreamWriter(filePath);
        this.linesPerCommit = linesPerCommit;
        dirty = false;

        for(int i = 0; i < columns; i++)
        {
            loggingString[i] = string.Empty;
        }
    }

    public void Log()
    {
        if (dirty)
        {
            string psring = string.Empty;
            for(int i = 0; i < columns; i ++)
            {
                psring += loggingString[i] + "\t";
            }
            writer.WriteLine(psring);
            linesWritten++;
            if (linesWritten > linesPerCommit)
            {
                linesWritten = 0;
                writer.Flush();
            }
            flush();
            dirty = false;
        }
    }

    public void setColumn(int column, string data)
    {
        if (!dirty)
        {
            dirty = true;
            loggingString[column] = data;
        }
        else if (!loggingString[column].Equals(string.Empty))
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
        writer.Flush();
        writer.Close();
    }
}
