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

        public static TacAnalyses.Model.ControlFlowGraph TransformToTac(MethodDefinition method, TacAnalyses.Model.ClassHierarchy classHierarchy = null)
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

            // The method body can be altered so we need to re compute the control flow graph. 
            CopyPropagationTransformation copyPropagation = new CopyPropagationTransformation(cfg);
            copyPropagation.Transform(methodBody);

            methodBody.UpdateVariables();

            // The method body can be altered so we need to re compute the control flow graph.
            var typeAnalysis = new LocalTypeInferenceAnalysis(method, methodBody, classHierarchy);
            typeAnalysis.Analyze();
            typeAnalysis.Transform();

            cfg = cfAnalysis.GenerateExceptionalControlFlow();

            methodBody.UpdateVariables();

            return cfg;
        }
    }
}
