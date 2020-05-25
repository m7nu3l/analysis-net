using TacAnalyses.Analyses;
using TacAnalyses.Transformations;
using Model.Types;
using System.IO;

namespace Tests
{
    internal class Utils
    {
        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static TacAnalyses.Model.ControlFlowGraph TransformToTac(MethodDefinition method)
        {
            Disassembler disassembler = new Disassembler(method);
            MethodBody methodBody = disassembler.Execute();
            method.Body = methodBody;

            ControlFlowAnalysis cfAnalysis = new ControlFlowAnalysis(method.Body);
            TacAnalyses.Model.ControlFlowGraph cfg = cfAnalysis.GenerateExceptionalControlFlow();

            WebAnalysis splitter = new WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            TypeInferenceAnalysis typeAnalysis = new TypeInferenceAnalysis(cfg, method.ReturnType);
            typeAnalysis.Analyze();

            methodBody.UpdateVariables();

            CopyPropagationTransformation copyPropagation = new CopyPropagationTransformation(cfg);
            copyPropagation.Transform(methodBody);

            methodBody.UpdateVariables();

            return cfg;
        }
    }
}
