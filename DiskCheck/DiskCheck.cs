/**
 *
 * Copyright (C) 2001-2019 eIrOcA (eNrIcO Croce & sImOnA Burzio) - AGPL >= 3.0
 *
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General
 * Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any
 * later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied
 * warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see
 * <http://www.gnu.org/licenses/>.
 *
 **/
using System;
using System.Collections.Specialized;
using System.IO;
using HttpUtils;
using LoggerUtils;

namespace DiskCheck {

  public class Notifier {

    public RestClient notifyRest = new RestClient();
    public RestClient notifyStepRest = new RestClient();

    public Notifier(NameValueCollection settings) {
      notifyStepRest.EndPoint = settings["notify_step"];
      notifyRest.EndPoint = settings["notify"];
      String use_eSysAdm = settings["eSysAdm_notify"];
      if (!String.IsNullOrWhiteSpace(use_eSysAdm) && (use_eSysAdm.ToLower().Equals("false"))) {
        notifyRest.EndPoint = null;
        notifyStepRest.EndPoint = null;
      }
      String proxy = settings["use_proxy"];
      bool useProxy = true;
      if (!String.IsNullOrWhiteSpace(proxy) && (proxy.ToLower().Equals("false"))) {
        useProxy = false;
      }
      notifyRest.useProxy = useProxy;
      notifyStepRest.useProxy = useProxy;
      Logger.Trace("Notify URL: {0}", notifyRest.EndPoint);
      Logger.Trace("Notify Step URL: {0}", notifyStepRest.EndPoint);
    }

    private string ReplaceNL(string msg) {
      return msg.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
    }

    public void NotifyStep(string name, int interation, double elapsed, bool success) {
      Logger.TracePerformanceDetail("{0:yyyyMMddHHmmss.} - Step: {1} Success: {3} Elapsed: {2}", DateTime.Now, interation, elapsed, success);
      if (notifyStepRest.EndPoint != null) {
        string response = notifyStepRest.MakeRequest(name, "" + elapsed, "" + interation, "" + success);
        Logger.Trace("Notify Step: {0}", ReplaceNL(response));
      }
    }

    public void Notify(string name, double connectionTime, double writeTime, bool success) {
      Logger.TracePerformance("{0:yyyyMMddHHmmss} - Success: {3} Connection Time: {1} Average Time: {2}", DateTime.Now, connectionTime, writeTime, success);
      if (notifyRest.EndPoint != null) {
        string response = notifyRest.MakeRequest(name, "" + writeTime, "" + connectionTime, "" + success);
        Logger.Trace("Notify: {0}", ReplaceNL(response));
      }
    }

  }

  public class PathChecker {

    public string testPath = @"C:\temp\testfile.dat";
    public int shortType = 2;
    public int testIterations = 3;
    public bool stopped = false;
    public int blocks = 50;

    public String domain;
    public String username;
    public String password;

    public String testName;

    private byte[] buffer_out = new byte[4096];
    private byte[] buffer_in = new byte[4096];

    public static int GetInt(String param, int defVal) {
      int value = defVal;
      Int32.TryParse(param, out value);
      return value;
    }
    public static string GetString(String param, string defVal) {
      if (String.IsNullOrWhiteSpace(param)) {
        return defVal;
      }
      return param;
    }

    public PathChecker(NameValueCollection settings) {
      for (int i = 0; i < buffer_out.Length; i++) {
        buffer_out[i] = (byte)(i % 2);
      }
      testPath = settings["testPath"];
      int size = GetInt(settings["size"], 1);
      blocks = (size + 4095) >> 12;
      if (blocks < 1) {
        blocks = 1;
      }
      testIterations = GetInt(settings["steps"], 1);
      domain = GetString(settings["domain"], null);
      username = GetString(settings["username"], null);
      password = GetString(settings["password"], null);
      testName = GetString(settings["name"], "check");
    }

    private bool WriteFile() {
      bool success = true;
      FileStream fileStream = null;
      try {
        fileStream = new FileStream(testPath, FileMode.Create, FileAccess.Write);
        for (int i = 1; i <= blocks; i++) {
          fileStream.Write(buffer_out, 0, buffer_out.Length);
        }
      }
      catch (Exception e) {
        Logger.Warning("Write expetion: {0}", e.Message);
        success = false;
      }
      finally {
        if (fileStream != null) {
          fileStream.Close();
        }
      }
      return success;
    }

    private bool ReadFile() {
      bool success = true;
      FileStream fileStream = null;
      try {
        fileStream = new FileStream(testPath, FileMode.Open, FileAccess.Read);
        for (int i = 1; i <= blocks; i++) {
          fileStream.Read(buffer_in, 0, buffer_in.Length);
        }
      }
      catch (Exception e) {
        Logger.Warning("Read expetion: {0}", e.Message);
        success = false;
      }
      finally {
        if (fileStream != null) {
          fileStream.Close();
        }
      }
      return success;
    }

    private void ExecuteStep(out TimeSpan elapsed, out bool success) {
      DateTime startTime;
      elapsed = new TimeSpan(0);
      success = false;
      if ((shortType & 2) == 2) {
        startTime = DateTime.Now;
        bool ok = WriteFile();
        elapsed += DateTime.Now - startTime;
        if (!ok) {
          return;
        }
      }
      if ((shortType & 1) == 1) {
        //Do Read test
        if (!File.Exists(testPath)) {
          if (!WriteFile()) {
            return;
          }
        }
        startTime = DateTime.Now;
        bool ok = ReadFile();
        if (!ok) {
          return;
        }
        elapsed += DateTime.Now - startTime;
      }
      success = true;
    }

    public double ExceupteTest(Notifier notifier) {
      Logger.Message("TestPath: {0} Size: {1}KiB Steps: {2}", testPath, blocks * 4, testIterations);
      TimeSpan totalTime = new TimeSpan(0);
      TimeSpan connectionTime = new TimeSpan(0);
      NetworkShareAccesser connect = null;
      String computerName = null;
      bool allSuccess = true;
      if (testPath.StartsWith(@"\\", StringComparison.Ordinal) && (username != null)) {
        int endPos = testPath.IndexOf('\\', 3);
        if (endPos < 1) {
          endPos = testPath.Length + 1;
        }
        computerName = testPath.Substring(2, endPos - 2);
        Logger.Trace("{0}", computerName);
        try {
          connect = NetworkShareAccesser.Access(computerName, domain, username, password);
          connectionTime = connect.connectTime;
        }
        catch (Exception e) {
          Logger.Warning("Connection expetion: {0}", e.Message);
          allSuccess = false;
        }
      }
      if (allSuccess) {
        for (int step = 1; step <= testIterations; step++) {
          if (stopped) {
            break;
          }
          TimeSpan elapsed = new TimeSpan(0);
          bool success = true;
          ExecuteStep(out elapsed, out success);
          allSuccess &= success;
          totalTime += elapsed;
          notifier.NotifyStep(testName, step, elapsed.TotalMilliseconds, success);
        }
        if (File.Exists(testPath)) {
          File.Delete(testPath);
        }
      }
      connect = null;
      notifier.Notify(testName, connectionTime.TotalMilliseconds, totalTime.TotalMilliseconds / testIterations, allSuccess);
      return totalTime.TotalMilliseconds;
    }

  }

}
