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
using System.Diagnostics;

namespace LoggerUtils {

  public class Logger {

    private static TraceSource tracing = new TraceSource("Tracing");
    private static TraceSource performance = new TraceSource("Performance");

    public static void Trace(string message, params object[] data) {
      tracing.TraceData(TraceEventType.Verbose, 0, String.Format(message, data));
    }

    public static void Message(string message, params object[] data) {
      tracing.TraceData(TraceEventType.Information, 1, String.Format(message, data));
    }

    public static void Warning(string message, params object[] data) {
      tracing.TraceData(TraceEventType.Warning, 2, String.Format(message, data));
    }

    public static void TracePerformanceDetail(string message, params object[] data) {
      performance.TraceData(TraceEventType.Verbose, 1, String.Format(message, data));
    }

    public static void TracePerformance(string message, params object[] data) {
      performance.TraceData(TraceEventType.Information, 0, String.Format(message, data));
    }

  }

}
