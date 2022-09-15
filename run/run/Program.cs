using System;                             //Console
using System.Diagnostics;                 //Process
using System.IO;                          //Path
using System.Runtime.Serialization.Json;  //JsonReaderWriterFactory
using System.Text;                        //Encoding
using System.Xml;                         //XmlDictionaryReader
using System.Runtime.Serialization;       //DataContract
using System.Globalization;               //CultureInfo

/* -------------------------------------------------------
 * read and write JSON file into an object.
 * no external dependencies. 
 * works on very old .net frameworks.
 * 
 * all very simple managed code. no string manipulation, no Windows API.
 * 
 * everything in one file, 
 * 
 * very little error handling 
 * to keep it a simple example you can actually read.
 * -------------------------------------------------------
 * 
 * project is set for .net 3.5 which is the minimal for having manifest 
 * built-into the build process.
 * you can probably set it to .net 2.0 if you want,  
 * and use https://github.com/eladkarako/manifest and '_embed__use_generic_manifest.cmd'  
 * to patch your exe after you build it.
 * 
 * -------------------------------------------------------
 * 
 * project is configured to copy 
 * 'run.json'
 * 'sol.exe'
 * 'cards.dll'
 * from the project folder
 * to whatever bin folder you have (release/debug) for testing purposes.
 * it uses build events and those 3 commands
 * copy /b /y "$(ProjectDir)\run.json" "$(TargetDir)" 
 * copy /b /y "$(ProjectDir)\sol.exe" "$(TargetDir)" 
 * copy /b /y "$(ProjectDir)\cards.dll" "$(TargetDir)" 
 * 
 * that's good old Windows 2000 Solitaire.
 */


//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Security.AccessControl;


namespace run
{
  [DataContract] public class JSON_Data{
    [DataMember] public string path;
    [DataMember] public string working_directory;
    [DataMember] public string json_was_last_updated_at;
  }

  class Program{
    //no need for 'public delegate void OnXmlDictionaryReaderClose(XmlDictionaryReader reader);' as it is ready in System.Xml, just create new callback and keep its "reference"
    public static void reader_close_callback(XmlDictionaryReader json_reader){
      //can't find a way to flush and close the streams. json_reader.Close();
      Console.WriteLine("reader_close_callback" 
                      + "\r\n" 
                      + "was called."
                      + "\r\n" 
                      + "no need to do anything in this method (unless you want to)."
                      + "\r\n" 
                      + "if you wish to (redundant) close the streams, just do it after (any time after .ReadObject is fine)."
                      );
    }
    public static OnXmlDictionaryReaderClose reader_close_callback_reference = reader_close_callback;

    static void Main(string[] args){
      string self_fully_qualified_path = Process.GetCurrentProcess().MainModule.FileName;  //.Split('\\').Last().Replace(".exe", ".js");

      /* --------------------------------------------
       * using .net framework v4.6.2 or newer?
       * adding @"\\?\" to the path allows working on very long paths.
       * 
       * it uses something called 'universal naming convention' (UNC),  
       * and perfectly fine to always include.
       * 
       * older .net frameworks (older than 4.6.2, and yes.. I've checked.. a lot..)
       * will just show you an error message while running, of invalid character in path.
       * 
       * in addition,  
       * the app.manifest includes long path awareness.  
       * in Win11 simply including it in the manifest will allow most WinAPI I/O related 
       * to be pretty much unrestrictive (your c# app uses those apis "in the background").
       * Windows 10 users will have to apply this registry patch:
       * 
       * Windows Registry Editor Version 5.00
       * [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
       * "LongPathsEnabled"=dword:00000001
       * 
       * .. to be able for the manifest entry to have any effect.
       * (otherwise it does nothing..).
       * 
       * ------------------------------------------------------------------------
       * best practice to include both. unless you are using old .net framework,  
       * in-which case, just use the manifest entry..
       * 
       * --------------------------------------------------------
       * a long explanation for this single line of text...
       * (which I've commented-out anyway since I'm using older .net for compatibility).
       */
        //self_fully_qualified_path = @"\\?\" + self_fully_qualified_path;

      
      
        self_fully_qualified_path        = Path.GetFullPath(self_fully_qualified_path);
      
        string default_working_directory = System.IO.Path.GetDirectoryName(self_fully_qualified_path);   //AppDomain.CurrentDomain.BaseDirectoryAppDomain.CurrentDomain.BaseDirectory
        string target_file               = System.IO.Path.GetFileNameWithoutExtension(self_fully_qualified_path) + ".json";

        FileStream file_stream_read = new FileStream(target_file
                                              , FileMode.Open          // how to open the file.
                                              , FileAccess.Read        // how to access the file, read only (minimal access).
                                              , FileShare.ReadWrite    // do not lock the file, this way other programs may read it/write to it as well.
                                              , 128                    // bufferSize in Bytes.
                                              , false                  // do not use async. this is a short program, there are no benefits to it.
                                              );
        BufferedStream buffered_stream_read = new BufferedStream(file_stream_read, 128); //replaces (not wraps) the buffering mechanism of the ReadStream. useful for older .net .
      
        /*
        //-------------------- testing code for just reading the json file as text.
        StreamReader stream_reader = new StreamReader(buffered_stream_read
                                              , Encoding.UTF8
                                              , true                   //auto recognize BOM. 
                                              , 128                    //description is ambigious https://github.com/dotnet/dotnet-api-docs/issues/1035#issuecomment-1244389393
                                              );
        string result = stream_reader.ReadToEnd();
        buffered_stream_read.Flush();
        try{
          stream_reader.DiscardBufferedData();
          stream_reader.Dispose();
          stream_reader.Close();
          buffered_stream_read.Dispose();
          buffered_stream_read.Close();
          file_stream_read.Dispose();
          file_stream_read.Close();
        }catch (System.Exception generic_exception) { }
        //----------------------------------------------------------
        */
      XmlDictionaryReader json_reader = JsonReaderWriterFactory.CreateJsonReader(
                                                        buffered_stream_read
                                                       ,Encoding.UTF8
                                                       ,XmlDictionaryReaderQuotas.Max
                                                       ,reader_close_callback_reference
                                                      );
      
      DataContractJsonSerializer json_text_to_object_serializer = new DataContractJsonSerializer( typeof(JSON_Data) );
      JSON_Data json_object = (JSON_Data)json_text_to_object_serializer.ReadObject(json_reader, false); //false - parsing less strictly. returns object, must be explicitly cast to whatever class you're using.

      try{ 
        buffered_stream_read.Flush();
        file_stream_read.Flush();
        json_reader.Close(); //yes, this one is before buffer close one, that is the order you should close things.
        buffered_stream_read.Close();
        
        file_stream_read.Dispose(); //probably redundant. 
        buffered_stream_read.Dispose(); //not redundant

        file_stream_read.Close();   //probably redundant. must be before buffered stream (quirks with old .net frameworks).
        buffered_stream_read.Close();
      }
      catch (Exception exception) {}

      //you can work with the json_object now, it has been populated.

      Console.WriteLine("data from json- path: " + json_object.path);
      
      Console.WriteLine("data from json- json_was_last_updated_at: " + json_object.json_was_last_updated_at);

      //----------------how to parse the "ISO" format I'm using in the json....
      DateTime date_time_json_was_last_updated_at = DateTime.ParseExact(
                                                  json_object.json_was_last_updated_at
                                                  ,"yyyyMMddTHHmmss"
                                                  ,new CultureInfo("en-US")
                                                  ,System.Globalization.DateTimeStyles.RoundtripKind
                                                  );
      Console.WriteLine(date_time_json_was_last_updated_at.ToString());

      
      //-------------------- optional, do something with the data from the json.. I'll run Win2k solitaire.
      Process process                    = new Process();
      process.StartInfo                  = new ProcessStartInfo();
      process.StartInfo.FileName         = json_object.path;
      process.StartInfo.WorkingDirectory = json_object.working_directory;
      process.StartInfo.WindowStyle      = ProcessWindowStyle.Maximized;
      
      /*-------------------------------------------------
       * use shell execute?
       * - false - create process as sub-process to this program from exe directly.
       *           arguments string with '%' character in it would not get corrupted.
       *           you need to specify exe, normally.
       * - true  - easier launch, can "launch" doc/txt even url 
       *           the shell will handle the default opening program if needed.
       *           sometimes the shell strips away '%' characters in the arguments string.  
       * -------------------
       * it will probably work fine either-way.
       * there are a lot of quirks that needs it either true or false.  
       * see: 
       * https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.useshellexecute
       * 
       * also, 
       * best to always explicitly set working directory,  
       * since the default sometimes goes to C:\Windows\System32
       * --------------------------------
       * rule of thumb: 
       * set working directory to same folder as the exe,  
       * set 'use shell execute' to false, to run exe directly.
       * 
       * if you need to set the window to maximize (or any other state)
       * set 'use shell execute' to true 
       * (note that the code has it set to false, and the window style has no effect..).
       */
      process.StartInfo.UseShellExecute  = false;
      process.Start();
      //process.WaitForExit();



      // --------------------------------------------------
      // write data (from the json object) to .json file:
      // just do the same, replace read with write 
      // in various method names.
      // 
      // a good test is to write the current date 
      // if you wish, 
      // the json_was_last_updated_at
      // is a good place.
      // the format "yyyyMMddTHHmmss"
      // will make sure you can ready it afterwards.
      // ---------------------------------------------
      string current_date_time = DateTime.Now.ToString("yyyyMMddTHHmmss");
      Console.WriteLine("current date time " + current_date_time + " (you can write it to the json if you wish, as a test..");




      Console.WriteLine("\r\n" + "[DONE] Press Enter to Exit . . ."); //"pause".
      Console.ReadLine(); //pause.
    }
  }
}
