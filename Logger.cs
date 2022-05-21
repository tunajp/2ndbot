namespace SecondBot.Client {
    public enum Level {
        INFO,
        WARN,
        ERROR,
    }
    public class Logger {
        string userDirectory;
        public Logger(string username) {
            this.userDirectory = username;
            this.mkdir(Constants.LOGDIR);
            this.userDirectory = Constants.LOGDIR + Path.DirectorySeparatorChar + this.userDirectory + Path.DirectorySeparatorChar;
            this.mkdir(this.userDirectory);
        }
        void mkdir(string dir) {
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }
        public void Info(string message) {
            Level l = Level.INFO;
            this.Logging(l.ToString() + ":" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ":" + message);
        }
        public void Warn(string message) {
            Level l = Level.WARN;
            this.Logging(l.ToString() + ":" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ":" + message);
        }
        public void Error(string message) {
            Level l = Level.ERROR;
            this.Logging(l.ToString() + ":" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ":" + message);
        }
        void Logging(string message) {
            try {
                string filename =  this.userDirectory + Constants.LOGFILE;
                using (StreamWriter writer = new StreamWriter(filename, true)) {
                    writer.WriteLine(message);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        public void ChatLog(string message) {
            try {
                string filename =  this.userDirectory + Constants.CHATLOGFILE;
                using (StreamWriter writer = new StreamWriter(filename, true)) {
                    writer.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ":" + message);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        public void IMLog(string from, string message) {
            try {
                string filename =  this.userDirectory + from + ".txt";
                using (StreamWriter writer = new StreamWriter(filename, true)) {
                    writer.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ":" + message);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}