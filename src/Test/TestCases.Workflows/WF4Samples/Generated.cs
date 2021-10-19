using System;

public class Calculation_CompiledExpressionRoot : System.Activities.XamlIntegration.ICompiledExpressionRoot {
    
    private System.Activities.Activity rootActivity;
    
    private object dataContextActivities;
    
    private bool forImplementation = true;
    
    public Calculation_CompiledExpressionRoot(System.Activities.Activity rootActivity) {
        if ((rootActivity == null)) {
            throw new System.ArgumentNullException("rootActivity");
        }
        this.rootActivity = rootActivity;
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public string GetLanguage() {
        return "C#";
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public object InvokeExpression(int expressionId, System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext) {
        if ((this.dataContextActivities == null)) {
            this.dataContextActivities = Calculation_CompiledExpressionRoot_TypedDataContext2.GetDataContextActivitiesHelper(this.rootActivity, this.forImplementation);
        }
        if ((expressionId == 0)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[0] == null)) {
                cachedCompiledDataContext[0] = new Calculation_CompiledExpressionRoot_TypedDataContext2(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2 refDataContext0 = ((Calculation_CompiledExpressionRoot_TypedDataContext2)(cachedCompiledDataContext[0]));
            return refDataContext0.GetLocation<int>(refDataContext0.ValueType___Expr0Get, refDataContext0.ValueType___Expr0Set, expressionId, this.rootActivity, activityContext);
        }
        if ((expressionId == 1)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[1] == null)) {
                cachedCompiledDataContext[1] = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext1 = ((Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly)(cachedCompiledDataContext[1]));
            return valDataContext1.ValueType___Expr1Get();
        }
        if ((expressionId == 2)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[1] == null)) {
                cachedCompiledDataContext[1] = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext2 = ((Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly)(cachedCompiledDataContext[1]));
            return valDataContext2.ValueType___Expr2Get();
        }
        if ((expressionId == 3)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[1] == null)) {
                cachedCompiledDataContext[1] = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext3 = ((Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly)(cachedCompiledDataContext[1]));
            return valDataContext3.ValueType___Expr3Get();
        }
        if ((expressionId == 4)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[1] == null)) {
                cachedCompiledDataContext[1] = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext4 = ((Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly)(cachedCompiledDataContext[1]));
            return valDataContext4.ValueType___Expr4Get();
        }
        if ((expressionId == 5)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[1] == null)) {
                cachedCompiledDataContext[1] = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext5 = ((Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly)(cachedCompiledDataContext[1]));
            return valDataContext5.ValueType___Expr5Get();
        }
        if ((expressionId == 6)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[1] == null)) {
                cachedCompiledDataContext[1] = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext6 = ((Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly)(cachedCompiledDataContext[1]));
            return valDataContext6.ValueType___Expr6Get();
        }
        if ((expressionId == 7)) {
            System.Activities.XamlIntegration.CompiledDataContext[] cachedCompiledDataContext = Calculation_CompiledExpressionRoot_TypedDataContext2.GetCompiledDataContextCacheHelper(this.dataContextActivities, activityContext, this.rootActivity, this.forImplementation, 2);
            if ((cachedCompiledDataContext[0] == null)) {
                cachedCompiledDataContext[0] = new Calculation_CompiledExpressionRoot_TypedDataContext2(locations, activityContext, true);
            }
            Calculation_CompiledExpressionRoot_TypedDataContext2 refDataContext7 = ((Calculation_CompiledExpressionRoot_TypedDataContext2)(cachedCompiledDataContext[0]));
            return refDataContext7.GetLocation<int>(refDataContext7.ValueType___Expr7Get, refDataContext7.ValueType___Expr7Set, expressionId, this.rootActivity, activityContext);
        }
        return null;
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public object InvokeExpression(int expressionId, System.Collections.Generic.IList<System.Activities.Location> locations) {
        if ((expressionId == 0)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2 refDataContext0 = new Calculation_CompiledExpressionRoot_TypedDataContext2(locations, true);
            return refDataContext0.GetLocation<int>(refDataContext0.ValueType___Expr0Get, refDataContext0.ValueType___Expr0Set);
        }
        if ((expressionId == 1)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext1 = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, true);
            return valDataContext1.ValueType___Expr1Get();
        }
        if ((expressionId == 2)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext2 = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, true);
            return valDataContext2.ValueType___Expr2Get();
        }
        if ((expressionId == 3)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext3 = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, true);
            return valDataContext3.ValueType___Expr3Get();
        }
        if ((expressionId == 4)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext4 = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, true);
            return valDataContext4.ValueType___Expr4Get();
        }
        if ((expressionId == 5)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext5 = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, true);
            return valDataContext5.ValueType___Expr5Get();
        }
        if ((expressionId == 6)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly valDataContext6 = new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locations, true);
            return valDataContext6.ValueType___Expr6Get();
        }
        if ((expressionId == 7)) {
            Calculation_CompiledExpressionRoot_TypedDataContext2 refDataContext7 = new Calculation_CompiledExpressionRoot_TypedDataContext2(locations, true);
            return refDataContext7.GetLocation<int>(refDataContext7.ValueType___Expr7Get, refDataContext7.ValueType___Expr7Set);
        }
        return null;
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public bool CanExecuteExpression(string expressionText, bool isReference, System.Collections.Generic.IList<System.Activities.LocationReference> locations, out int expressionId) {
        if (((isReference == true) 
                    && ((expressionText == "multiplyResult") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2.Validate(locations, true, 0) == true)))) {
            expressionId = 0;
            return true;
        }
        if (((isReference == false) 
                    && ((expressionText == "XX") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.Validate(locations, true, 0) == true)))) {
            expressionId = 1;
            return true;
        }
        if (((isReference == false) 
                    && ((expressionText == "YY") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.Validate(locations, true, 0) == true)))) {
            expressionId = 2;
            return true;
        }
        if (((isReference == false) 
                    && ((expressionText == "multiplyResult==256") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.Validate(locations, true, 0) == true)))) {
            expressionId = 3;
            return true;
        }
        if (((isReference == false) 
                    && ((expressionText == "XX==YY") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.Validate(locations, true, 0) == true)))) {
            expressionId = 4;
            return true;
        }
        if (((isReference == false) 
                    && ((expressionText == "TimeSpan.FromMilliseconds(0)") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.Validate(locations, true, 0) == true)))) {
            expressionId = 5;
            return true;
        }
        if (((isReference == false) 
                    && ((expressionText == "multiplyResult") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly.Validate(locations, true, 0) == true)))) {
            expressionId = 6;
            return true;
        }
        if (((isReference == true) 
                    && ((expressionText == "Result") 
                    && (Calculation_CompiledExpressionRoot_TypedDataContext2.Validate(locations, true, 0) == true)))) {
            expressionId = 7;
            return true;
        }
        expressionId = -1;
        return false;
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public System.Collections.Generic.IList<string> GetRequiredLocations(int expressionId) {
        return null;
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public System.Linq.Expressions.Expression GetExpressionTreeForExpression(int expressionId, System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) {
        if ((expressionId == 0)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2(locationReferences).@__Expr0GetTree();
        }
        if ((expressionId == 1)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locationReferences).@__Expr1GetTree();
        }
        if ((expressionId == 2)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locationReferences).@__Expr2GetTree();
        }
        if ((expressionId == 3)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locationReferences).@__Expr3GetTree();
        }
        if ((expressionId == 4)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locationReferences).@__Expr4GetTree();
        }
        if ((expressionId == 5)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locationReferences).@__Expr5GetTree();
        }
        if ((expressionId == 6)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(locationReferences).@__Expr6GetTree();
        }
        if ((expressionId == 7)) {
            return new Calculation_CompiledExpressionRoot_TypedDataContext2(locationReferences).@__Expr7GetTree();
        }
        return null;
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    private class Calculation_CompiledExpressionRoot_TypedDataContext0 : System.Activities.XamlIntegration.CompiledDataContext {
        
        private int locationsOffset;
        
        private static int expectedLocationsCount;
        
        public Calculation_CompiledExpressionRoot_TypedDataContext0(System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext, bool computelocationsOffset) : 
                base(locations, activityContext) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext0(System.Collections.Generic.IList<System.Activities.Location> locations, bool computelocationsOffset) : 
                base(locations) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext0(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) : 
                base(locationReferences) {
        }
        
        internal static object GetDataContextActivitiesHelper(System.Activities.Activity compiledRoot, bool forImplementation) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetDataContextActivities(compiledRoot, forImplementation);
        }
        
        internal static System.Activities.XamlIntegration.CompiledDataContext[] GetCompiledDataContextCacheHelper(object dataContextActivities, System.Activities.ActivityContext activityContext, System.Activities.Activity compiledRoot, bool forImplementation, int compiledDataContextCount) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetCompiledDataContextCache(dataContextActivities, activityContext, compiledRoot, forImplementation, compiledDataContextCount);
        }
        
        public virtual void SetLocationsOffset(int locationsOffsetValue) {
            locationsOffset = locationsOffsetValue;
        }
        
        public static bool Validate(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences, bool validateLocationCount, int offset) {
            if (((validateLocationCount == true) 
                        && (locationReferences.Count < 0))) {
                return false;
            }
            expectedLocationsCount = 0;
            return true;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    private class Calculation_CompiledExpressionRoot_TypedDataContext0_ForReadOnly : System.Activities.XamlIntegration.CompiledDataContext {
        
        private int locationsOffset;
        
        private static int expectedLocationsCount;
        
        public Calculation_CompiledExpressionRoot_TypedDataContext0_ForReadOnly(System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext, bool computelocationsOffset) : 
                base(locations, activityContext) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext0_ForReadOnly(System.Collections.Generic.IList<System.Activities.Location> locations, bool computelocationsOffset) : 
                base(locations) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext0_ForReadOnly(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) : 
                base(locationReferences) {
        }
        
        internal static object GetDataContextActivitiesHelper(System.Activities.Activity compiledRoot, bool forImplementation) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetDataContextActivities(compiledRoot, forImplementation);
        }
        
        internal static System.Activities.XamlIntegration.CompiledDataContext[] GetCompiledDataContextCacheHelper(object dataContextActivities, System.Activities.ActivityContext activityContext, System.Activities.Activity compiledRoot, bool forImplementation, int compiledDataContextCount) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetCompiledDataContextCache(dataContextActivities, activityContext, compiledRoot, forImplementation, compiledDataContextCount);
        }
        
        public virtual void SetLocationsOffset(int locationsOffsetValue) {
            locationsOffset = locationsOffsetValue;
        }
        
        public static bool Validate(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences, bool validateLocationCount, int offset) {
            if (((validateLocationCount == true) 
                        && (locationReferences.Count < 0))) {
                return false;
            }
            expectedLocationsCount = 0;
            return true;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    private class Calculation_CompiledExpressionRoot_TypedDataContext1 : Calculation_CompiledExpressionRoot_TypedDataContext0 {
        
        private int locationsOffset;
        
        private static int expectedLocationsCount;
        
        protected int YY;
        
        protected int XX;
        
        protected int Result;
        
        public Calculation_CompiledExpressionRoot_TypedDataContext1(System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext, bool computelocationsOffset) : 
                base(locations, activityContext, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext1(System.Collections.Generic.IList<System.Activities.Location> locations, bool computelocationsOffset) : 
                base(locations, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext1(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) : 
                base(locationReferences) {
        }
        
        internal new static System.Activities.XamlIntegration.CompiledDataContext[] GetCompiledDataContextCacheHelper(object dataContextActivities, System.Activities.ActivityContext activityContext, System.Activities.Activity compiledRoot, bool forImplementation, int compiledDataContextCount) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetCompiledDataContextCache(dataContextActivities, activityContext, compiledRoot, forImplementation, compiledDataContextCount);
        }
        
        public new virtual void SetLocationsOffset(int locationsOffsetValue) {
            locationsOffset = locationsOffsetValue;
            base.SetLocationsOffset(locationsOffset);
        }
        
        protected override void GetValueTypeValues() {
            this.YY = ((int)(this.GetVariableValue((0 + locationsOffset))));
            this.XX = ((int)(this.GetVariableValue((1 + locationsOffset))));
            this.Result = ((int)(this.GetVariableValue((2 + locationsOffset))));
            base.GetValueTypeValues();
        }
        
        protected override void SetValueTypeValues() {
            this.SetVariableValue((0 + locationsOffset), this.YY);
            this.SetVariableValue((1 + locationsOffset), this.XX);
            this.SetVariableValue((2 + locationsOffset), this.Result);
            base.SetValueTypeValues();
        }
        
        public new static bool Validate(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences, bool validateLocationCount, int offset) {
            if (((validateLocationCount == true) 
                        && (locationReferences.Count < 3))) {
                return false;
            }
            if ((validateLocationCount == true)) {
                offset = (locationReferences.Count - 3);
            }
            expectedLocationsCount = 3;
            if (((locationReferences[(offset + 0)].Name != "YY") 
                        || (locationReferences[(offset + 0)].Type != typeof(int)))) {
                return false;
            }
            if (((locationReferences[(offset + 1)].Name != "XX") 
                        || (locationReferences[(offset + 1)].Type != typeof(int)))) {
                return false;
            }
            if (((locationReferences[(offset + 2)].Name != "Result") 
                        || (locationReferences[(offset + 2)].Type != typeof(int)))) {
                return false;
            }
            return Calculation_CompiledExpressionRoot_TypedDataContext0.Validate(locationReferences, false, offset);
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    private class Calculation_CompiledExpressionRoot_TypedDataContext1_ForReadOnly : Calculation_CompiledExpressionRoot_TypedDataContext0_ForReadOnly {
        
        private int locationsOffset;
        
        private static int expectedLocationsCount;
        
        protected int YY;
        
        protected int XX;
        
        protected int Result;
        
        public Calculation_CompiledExpressionRoot_TypedDataContext1_ForReadOnly(System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext, bool computelocationsOffset) : 
                base(locations, activityContext, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext1_ForReadOnly(System.Collections.Generic.IList<System.Activities.Location> locations, bool computelocationsOffset) : 
                base(locations, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext1_ForReadOnly(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) : 
                base(locationReferences) {
        }
        
        internal new static System.Activities.XamlIntegration.CompiledDataContext[] GetCompiledDataContextCacheHelper(object dataContextActivities, System.Activities.ActivityContext activityContext, System.Activities.Activity compiledRoot, bool forImplementation, int compiledDataContextCount) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetCompiledDataContextCache(dataContextActivities, activityContext, compiledRoot, forImplementation, compiledDataContextCount);
        }
        
        public new virtual void SetLocationsOffset(int locationsOffsetValue) {
            locationsOffset = locationsOffsetValue;
            base.SetLocationsOffset(locationsOffset);
        }
        
        protected override void GetValueTypeValues() {
            this.YY = ((int)(this.GetVariableValue((0 + locationsOffset))));
            this.XX = ((int)(this.GetVariableValue((1 + locationsOffset))));
            this.Result = ((int)(this.GetVariableValue((2 + locationsOffset))));
            base.GetValueTypeValues();
        }
        
        public new static bool Validate(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences, bool validateLocationCount, int offset) {
            if (((validateLocationCount == true) 
                        && (locationReferences.Count < 3))) {
                return false;
            }
            if ((validateLocationCount == true)) {
                offset = (locationReferences.Count - 3);
            }
            expectedLocationsCount = 3;
            if (((locationReferences[(offset + 0)].Name != "YY") 
                        || (locationReferences[(offset + 0)].Type != typeof(int)))) {
                return false;
            }
            if (((locationReferences[(offset + 1)].Name != "XX") 
                        || (locationReferences[(offset + 1)].Type != typeof(int)))) {
                return false;
            }
            if (((locationReferences[(offset + 2)].Name != "Result") 
                        || (locationReferences[(offset + 2)].Type != typeof(int)))) {
                return false;
            }
            return Calculation_CompiledExpressionRoot_TypedDataContext0_ForReadOnly.Validate(locationReferences, false, offset);
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    private class Calculation_CompiledExpressionRoot_TypedDataContext2 : Calculation_CompiledExpressionRoot_TypedDataContext1 {
        
        private int locationsOffset;
        
        private static int expectedLocationsCount;
        
        protected int multiplyResult;
        
        public Calculation_CompiledExpressionRoot_TypedDataContext2(System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext, bool computelocationsOffset) : 
                base(locations, activityContext, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext2(System.Collections.Generic.IList<System.Activities.Location> locations, bool computelocationsOffset) : 
                base(locations, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext2(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) : 
                base(locationReferences) {
        }
        
        internal new static System.Activities.XamlIntegration.CompiledDataContext[] GetCompiledDataContextCacheHelper(object dataContextActivities, System.Activities.ActivityContext activityContext, System.Activities.Activity compiledRoot, bool forImplementation, int compiledDataContextCount) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetCompiledDataContextCache(dataContextActivities, activityContext, compiledRoot, forImplementation, compiledDataContextCount);
        }
        
        public new virtual void SetLocationsOffset(int locationsOffsetValue) {
            locationsOffset = locationsOffsetValue;
            base.SetLocationsOffset(locationsOffset);
        }
        
        internal System.Linq.Expressions.Expression @__Expr0GetTree() {
            System.Linq.Expressions.Expression<System.Func<int>> expression = () => multiplyResult;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public int @__Expr0Get() {
            return multiplyResult;
        }
        
        public int ValueType___Expr0Get() {
            this.GetValueTypeValues();
            return this.@__Expr0Get();
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public void @__Expr0Set(int value) {
            multiplyResult = value;
        }
        
        public void ValueType___Expr0Set(int value) {
            this.GetValueTypeValues();
            this.@__Expr0Set(value);
            this.SetValueTypeValues();
        }
        
        internal System.Linq.Expressions.Expression @__Expr7GetTree() {
            System.Linq.Expressions.Expression<System.Func<int>> expression = () => Result;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public int @__Expr7Get() {
            return Result;
        }
        
        public int ValueType___Expr7Get() {
            this.GetValueTypeValues();
            return this.@__Expr7Get();
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public void @__Expr7Set(int value) {
            Result = value;
        }
        
        public void ValueType___Expr7Set(int value) {
            this.GetValueTypeValues();
            this.@__Expr7Set(value);
            this.SetValueTypeValues();
        }
        
        protected override void GetValueTypeValues() {
            this.multiplyResult = ((int)(this.GetVariableValue((3 + locationsOffset))));
            base.GetValueTypeValues();
        }
        
        protected override void SetValueTypeValues() {
            this.SetVariableValue((3 + locationsOffset), this.multiplyResult);
            base.SetValueTypeValues();
        }
        
        public new static bool Validate(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences, bool validateLocationCount, int offset) {
            if (((validateLocationCount == true) 
                        && (locationReferences.Count < 4))) {
                return false;
            }
            if ((validateLocationCount == true)) {
                offset = (locationReferences.Count - 4);
            }
            expectedLocationsCount = 4;
            if (((locationReferences[(offset + 3)].Name != "multiplyResult") 
                        || (locationReferences[(offset + 3)].Type != typeof(int)))) {
                return false;
            }
            return Calculation_CompiledExpressionRoot_TypedDataContext1.Validate(locationReferences, false, offset);
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("UiPath.Workflow", "1.0.0.0")]
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    private class Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly : Calculation_CompiledExpressionRoot_TypedDataContext1_ForReadOnly {
        
        private int locationsOffset;
        
        private static int expectedLocationsCount;
        
        protected int multiplyResult;
        
        public Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(System.Collections.Generic.IList<System.Activities.LocationReference> locations, System.Activities.ActivityContext activityContext, bool computelocationsOffset) : 
                base(locations, activityContext, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(System.Collections.Generic.IList<System.Activities.Location> locations, bool computelocationsOffset) : 
                base(locations, false) {
            if ((computelocationsOffset == true)) {
                this.SetLocationsOffset((locations.Count - expectedLocationsCount));
            }
        }
        
        public Calculation_CompiledExpressionRoot_TypedDataContext2_ForReadOnly(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences) : 
                base(locationReferences) {
        }
        
        internal new static System.Activities.XamlIntegration.CompiledDataContext[] GetCompiledDataContextCacheHelper(object dataContextActivities, System.Activities.ActivityContext activityContext, System.Activities.Activity compiledRoot, bool forImplementation, int compiledDataContextCount) {
            return System.Activities.XamlIntegration.CompiledDataContext.GetCompiledDataContextCache(dataContextActivities, activityContext, compiledRoot, forImplementation, compiledDataContextCount);
        }
        
        public new virtual void SetLocationsOffset(int locationsOffsetValue) {
            locationsOffset = locationsOffsetValue;
            base.SetLocationsOffset(locationsOffset);
        }
        
        internal System.Linq.Expressions.Expression @__Expr1GetTree() {
            System.Linq.Expressions.Expression<System.Func<int>> expression = () => XX;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public int @__Expr1Get() {
            return XX;
        }
        
        public int ValueType___Expr1Get() {
            this.GetValueTypeValues();
            return this.@__Expr1Get();
        }
        
        internal System.Linq.Expressions.Expression @__Expr2GetTree() {
            System.Linq.Expressions.Expression<System.Func<int>> expression = () => YY;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public int @__Expr2Get() {
            return YY;
        }
        
        public int ValueType___Expr2Get() {
            this.GetValueTypeValues();
            return this.@__Expr2Get();
        }
        
        internal System.Linq.Expressions.Expression @__Expr3GetTree() {
            System.Linq.Expressions.Expression<System.Func<bool>> expression = () => multiplyResult==256;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public bool @__Expr3Get() {
            return multiplyResult==256;
        }
        
        public bool ValueType___Expr3Get() {
            this.GetValueTypeValues();
            return this.@__Expr3Get();
        }
        
        internal System.Linq.Expressions.Expression @__Expr4GetTree() {
            System.Linq.Expressions.Expression<System.Func<bool>> expression = () => XX==YY;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public bool @__Expr4Get() {
            return XX==YY;
        }
        
        public bool ValueType___Expr4Get() {
            this.GetValueTypeValues();
            return this.@__Expr4Get();
        }
        
        internal System.Linq.Expressions.Expression @__Expr5GetTree() {
            System.Linq.Expressions.Expression<System.Func<System.TimeSpan>> expression = () => TimeSpan.FromMilliseconds(0);
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public System.TimeSpan @__Expr5Get() {
            return TimeSpan.FromMilliseconds(0);
        }
        
        public System.TimeSpan ValueType___Expr5Get() {
            this.GetValueTypeValues();
            return this.@__Expr5Get();
        }
        
        internal System.Linq.Expressions.Expression @__Expr6GetTree() {
            System.Linq.Expressions.Expression<System.Func<int>> expression = () => multiplyResult;
            return base.RewriteExpressionTree(expression);
        }
        
        [System.Diagnostics.DebuggerHiddenAttribute()]
        public int @__Expr6Get() {
            return multiplyResult;
        }
        
        public int ValueType___Expr6Get() {
            this.GetValueTypeValues();
            return this.@__Expr6Get();
        }
        
        protected override void GetValueTypeValues() {
            this.multiplyResult = ((int)(this.GetVariableValue((3 + locationsOffset))));
            base.GetValueTypeValues();
        }
        
        public new static bool Validate(System.Collections.Generic.IList<System.Activities.LocationReference> locationReferences, bool validateLocationCount, int offset) {
            if (((validateLocationCount == true) 
                        && (locationReferences.Count < 4))) {
                return false;
            }
            if ((validateLocationCount == true)) {
                offset = (locationReferences.Count - 4);
            }
            expectedLocationsCount = 4;
            if (((locationReferences[(offset + 3)].Name != "multiplyResult") 
                        || (locationReferences[(offset + 3)].Type != typeof(int)))) {
                return false;
            }
            return Calculation_CompiledExpressionRoot_TypedDataContext1_ForReadOnly.Validate(locationReferences, false, offset);
        }
    }
}
