/**
 *
 * Copyright (C) 2001-2019 Enrico Croce - AGPL >= 3.0
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
using System.Collections.Specialized;
using System.Configuration;

namespace DiskCheck {

  class Program {

    public static int Main(string[] args) {
      NameValueCollection checkSetting = null;
      NameValueCollection notifySettings = null;
      if (args.Length >= 1) {
        checkSetting = new NameValueCollection();
        notifySettings = new NameValueCollection();
        checkSetting.Add("testpath", args[0]);
        if (args.Length >= 2) {
          checkSetting.Add("size", args[1]);
        }
        if (args.Length >= 3) {
          checkSetting.Add("steps", args[2]);
        }
        if (args.Length >= 4) {
          notifySettings.Add("notify", args[3]);
        }
      }
      if (notifySettings == null) {
        notifySettings = ConfigurationManager.GetSection("Notify") as NameValueCollection;
      }
      // Result notifier
      Notifier notifier = new Notifier(notifySettings);
      // Execute the speed test
      double time = 0;
      if (checkSetting != null) {
        // data on command line
        PathChecker pathCheck = new PathChecker(checkSetting);
        time += pathCheck.ExceupteTest(notifier);
      }
      else {
        // data on application configuration section 
        int i = 0;
        do {
          string sectionName = "Check" + (i > 0 ? "" + i : "");
          checkSetting = ConfigurationManager.GetSection(sectionName) as NameValueCollection;
          if (checkSetting == null) {
            break;
          }
          PathChecker pathCheck = new PathChecker(checkSetting);
          time += pathCheck.ExceupteTest(notifier);
          i++;
        }
        while (true);
      }
      return (int)time;
    }

  }

}