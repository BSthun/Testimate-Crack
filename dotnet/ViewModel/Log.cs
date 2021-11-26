// Decompiled with JetBrains decompiler
// Type: Testiamte.ViewModel.Log
// Assembly: Testiamte, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1835882B-FCD8-4F86-8423-F7808C83A056
// Assembly location: D:\Testimate\Testimate\bin\Testiamte.dll

using System;
using Testiamte.Enum;

namespace Testiamte.ViewModel
{
    public class Log
    {
        public string ExamTestResultId { get; set; }

        public string Name { get; set; }

        public LogUsageType LogUsageType { get; set; }

        public DateTime? StartDate { get; set; }
    }
}
