// Decompiled with JetBrains decompiler
// Type: Testiamte.ViewModel.Capture
// Assembly: Testiamte, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1835882B-FCD8-4F86-8423-F7808C83A056
// Assembly location: D:\Testimate\Testimate\bin\Testiamte.dll

using System;

namespace Testiamte.ViewModel
{
    public class Capture
    {
        public string UserId { get; set; }

        public string ExamTestResultId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public UploadFile UserCapture { get; set; }

        public UploadFile ScreenCapture { get; set; }

        public DateTime Created { get; set; }
    }
}
