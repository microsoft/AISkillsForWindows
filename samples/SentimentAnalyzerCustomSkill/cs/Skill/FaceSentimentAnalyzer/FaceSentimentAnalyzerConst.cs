// Copyright (c) Microsoft Corporation. All rights reserved. 

using System.Collections.Generic;

namespace Contoso.FaceSentimentAnalyzer
{
    /// <summary>
    /// Set of contant values used throughout the skill
    /// </summary>
    internal static class FaceSentimentAnalyzerConst
    {
        public const string WINML_MODEL_FILENAME = "emotion_ferplus.onnx";
        public const string WINML_MODEL_INPUTNAME = "Input3";
        public const string WINML_MODEL_OUTPUTNAME = "Plus692_Output_0";
        public const string SKILL_INPUTNAME_IMAGE = "InputImage";
        public const string SKILL_OUTPUTNAME_FACEBOUNDINGBOXES = "FaceBoundingBoxes";
        public const string SKILL_OUTPUTNAME_FACESENTIMENTSSCORES = "FaceSentimentsScores";
    }
}
