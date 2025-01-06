using System.IO;

namespace ET
{
    public static class AnalyzeAssembly
    {
        private const string Core = "ET.Core";
        private const string Model = "ET.Model";
        private const string ClientModel = "ETClient.Model";
        private const string Hotfix = "ET.Hotfix";
        private const string ClientHotfix = "ETClient.Hotfix";
        private const string ModelView = "ET.ModelView";
        private const string ClientModelView = "ETClient.ModelView";
        private const string HotfixView = "ET.HotfixView";
        private const string ClientHotfixView = "ETClient.HotfixView";

        public static readonly string[] AllHotfix =
        [
            Hotfix, HotfixView,ClientHotfix,ClientHotfixView
        ];

        public static readonly string[] AllModel =
        [
            Model, ModelView, ClientModel, ClientModelView
        ];

        public static readonly string[] AllModelHotfix =
        [
            Model, Hotfix, ModelView, HotfixView, ClientModel, ClientHotfix, ClientModelView, ClientHotfixView
        ];
        
        public static readonly string[] All =
        [
            Core, Model, Hotfix, ModelView, HotfixView, ClientModel, ClientHotfix, ClientModelView, ClientHotfixView
        ];
        
        public static readonly string[] AllLogicModel =
        [
            Model,ClientModel
        ];
    }
    
}