using System;
using CommandLineExecution;
using VersionControl;

namespace VersionControl.Backend.P4
{
	public class P4Util
	{
		public class P4Vars 
		{
	        public string userName = "";
	        public string password = "";
			public string clientSpec = "";
			public string port = "";
			public string configFile = ".p4config";
			public string ignoreFile = ".p4ignore"; 
			public string workingDirectory = ".";
			public string cliEnding = "";
		}
		
		private static P4Util s_Instance = null;

		private readonly object operationActiveLockToken = new object();
		private P4Vars p4Vars = new P4Vars();
		
		static public P4Util Instance {
			get {
				if ( s_Instance == null ) s_Instance = new P4Util();
				return s_Instance;
			}
		}
		
		public P4Vars Vars {
			get {
				return p4Vars;
			}
		}
		
		public bool P4Initialized {
			get { return !(String.IsNullOrEmpty(p4Vars.userName) || String.IsNullOrEmpty(p4Vars.port)); }
		}

		private P4Util ()
		{
		}

	    public CommandLine CreateP4CommandLine(string arguments, string input = null)
	    {
	        if (P4Initialized)
	        {
	            arguments = " -u " + p4Vars.userName
					      + (String.IsNullOrEmpty(p4Vars.password) ? "" : " -P " + p4Vars.password)
						  + (String.IsNullOrEmpty(p4Vars.clientSpec) ? "" : " -c " + p4Vars.clientSpec)
						  + " -p " + p4Vars.port + " " + arguments;
	        }
	        return new CommandLine("p4", arguments, p4Vars.workingDirectory, input, p4Vars.cliEnding);
	    }

		public void P4Set(string key, string value) {
	        CommandLineOutput commandLineOutput;
	        using (var p4SetTask = CreateP4CommandLine("set " + key + "=" + value))
	        {
				D.Log("CMD: " + p4SetTask.ToString());
	            commandLineOutput = ExecuteOperation(p4SetTask);
				if ( !String.IsNullOrEmpty(commandLineOutput.OutputStr) ) {
					D.Log("P4: " + commandLineOutput.OutputStr);
				}
			}
		}

	    public CommandLineOutput ExecuteOperation(CommandLine commandLine, bool useOperationLock = true)
	    {
	        CommandLineOutput commandLineOutput;
	        if (useOperationLock)
	        {
	            lock (operationActiveLockToken)
	            {
	                commandLineOutput = ExecuteCommandLine(commandLine);
	            }
	        }
	        else
	        {
	            commandLineOutput = ExecuteCommandLine(commandLine);
	        }
			
			//D.Log(commandLineOutput + " " + commandLine.ToString());
			if ( !String.IsNullOrEmpty(commandLineOutput.OutputStr) ) {
		        if (commandLineOutput.Arguments.Contains("ExceptionTest.txt"))
		        {
		            D.Log("ERROR: Test Exception cast due to ExceptionTest.txt being a part of arguments");
		        }
		        if (!string.IsNullOrEmpty(commandLineOutput.ErrorStr))
		        {
		            var errStr = commandLineOutput.ErrorStr;
		            if (errStr.Contains("E730060") || errStr.Contains("Unable to connect") || errStr.Contains("is unreachable") || errStr.Contains("Operation timed out") || errStr.Contains("Can't connect to"))
		                D.Log("ERROR: " + errStr + " " + commandLine.ToString());
		            if (errStr.Contains("W160042") || errStr.Contains("Newer Version"))
		                D.Log("ERROR: " + errStr + " " + commandLine.ToString());
		            if (errStr.Contains("W155007") || errStr.Contains("'" + p4Vars.workingDirectory + "'" + " is not a working copy"))
		                D.Log("ERROR: " + errStr + " " + commandLine.ToString());
		            if (errStr.Contains("E160028") || errStr.Contains("is out of date"))
		                D.Log("ERROR: " + errStr + " " + commandLine.ToString());
		            if (errStr.Contains("E155037") || errStr.Contains("E155004") || errStr.Contains("run 'p4 cleanup'"))
		                D.Log("ERROR: " + errStr + " " + commandLine.ToString());
		            if (errStr.Contains("W160035") || errStr.Contains("is already locked by user"))
		                D.Log("ERROR: " + errStr + " " + commandLine.ToString());
					D.Log("ERROR: " + errStr + " " + commandLine.ToString());
		        }
			}
	        return commandLineOutput;
	    }
		
	    private CommandLineOutput ExecuteCommandLine(CommandLine commandLine)
	    {
	        CommandLineOutput commandLineOutput;
	        try
	        {
	            //Debug.Log(commandLine.ToString());
	            commandLineOutput = commandLine.Execute();
		        return commandLineOutput;
	        }
	        catch (Exception e)
	        {
	            D.Log("ERROR: Check that your commandline P4 client is installed correctly - " + e.Message);
	        }
	        return null;
	    }
	
		public void InitVars()
		{
	        CommandLineOutput commandLineOutput;
			
			// get connection info
	        using (var p4StatusTask = CreateP4CommandLine("set"))
	        {
				D.Log("CMD: " + p4StatusTask.ToString());
	            commandLineOutput = ExecuteOperation(p4StatusTask);
				if ( !String.IsNullOrEmpty(commandLineOutput.OutputStr) ) {
					string[] output = commandLineOutput.OutputStr.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
					D.Log("P4: " + commandLineOutput.OutputStr);
					// sample output:
					// P4CLIENT=workspace_name (set)
					// P4EDITOR=C:\Program Files (x86)\Notepad++\notepad++.exe (set)
					// P4PASSWD=password (set)
					// P4PORT=192.168.1.1:1666
					// P4USER=username
					foreach( String line in output ) {
						var cleaned = line.Trim();
						// check for/remove (set) and (config) tags
						if ( line.IndexOf("(set") != -1 ) {
							cleaned = line.Substring(0, line.IndexOf("(set")).Trim();
						}
						else if ( line.IndexOf("(config") != -1 ) {
							cleaned = line.Substring(0, line.IndexOf("(config")).Trim();
						}
		
						if ( cleaned.StartsWith("P4CLIENT=") )
						{
							p4Vars.clientSpec = cleaned.Substring( "P4CLIENT=".Length );
							//Debug.Log(window.p4Vars.clientSpec);
						}
						else if ( cleaned.StartsWith("P4PASSWD=") )
						{
							p4Vars.password = cleaned.Substring( "P4PASSWD=".Length );
							//Debug.Log(window.p4Vars.password);
						}
						else if ( cleaned.StartsWith("P4PORT=") )
						{
							p4Vars.port = cleaned.Substring( "P4PORT=".Length );
							//Debug.Log(window.p4Vars.port);
						}
						else if ( cleaned.StartsWith("P4USER=") )
						{
							p4Vars.userName = cleaned.Substring( "P4USER=".Length );
							//Debug.Log(window.p4Vars.userName);
						}
						else if ( cleaned.StartsWith("P4CONFIG=") )
						{
							p4Vars.configFile = cleaned.Substring( "P4CONFIG=".Length );
							//Debug.Log(window.p4Vars.configFile);
						}
						else if ( cleaned.StartsWith("P4IGNORE=") )
						{
							p4Vars.ignoreFile = cleaned.Substring( "P4IGNORE=".Length );
							//Debug.Log(window.p4Vars.ignoreFile);
						}
					}
				}
			}
		}
	}
}

