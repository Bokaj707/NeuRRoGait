
class Logger
{
    string[] loggingString;
    int linesWritten;
    int columns;
    string filePath;
    string fileName;
    int linesPerCommit;
    System.IO.StreamWriter writer;
    bool dirty;
    bool logging;
    float time;
    float lastTime;
    LoggingButtonHandler handler;

    public Logger(int columns, string filepath, string filename, int linesPerCommit, LoggingButtonHandler handler)
    {
        construct(columns, filepath, filename, linesPerCommit, handler);
    }
    public Logger(int columns, string filepath, string filename, int linesPerCommit, string[] headers, LoggingButtonHandler handler)
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
        loggingString = new string[columns];
        linesWritten = 0;
        this.columns = columns;
        SetFileName(filename);
        SetFilePath(filepath);
        this.handler = handler;

        writer = new System.IO.StreamWriter(filePath+fileName);
        handler.SetDefaultPaths(filePath, fileName);
        this.linesPerCommit = linesPerCommit;
        dirty = false;
        logging = false;
        time = 0;

        for(int i = 0; i < columns; i++)
        {
            loggingString[i] = string.Empty;
        }
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
                if (linesWritten > linesPerCommit)
                {
                    linesWritten = 0;
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
        writer.Flush();
        writer.Close();
    }
    public bool toggleLogging()
    {
        logging = !logging;
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
    public interface LoggingButtonHandler
    {
        void LoggingButtonPress();
        void SetDefaultPaths(string filePath, string fileName);
    }
}
