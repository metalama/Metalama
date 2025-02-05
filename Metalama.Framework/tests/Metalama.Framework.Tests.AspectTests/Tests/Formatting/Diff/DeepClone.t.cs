internal class Targets
    {
        [DeepClone]
        private class AutomaticallyCloneable : ICloneable
        {
            private int a;

            private ManuallyCloneable? b;

            private AutomaticallyCloneable? c;

            public virtual AutomaticallyCloneable Clone()
            {
                var clone = (AutomaticallyCloneable)MemberwiseClone();
                clone.b = (ManuallyCloneable?)((ICloneable)this.b).Clone();
                clone.c = (AutomaticallyCloneable)((ICloneable)this.c).Clone();
                return clone;
            }

            private AutomaticallyCloneable Clone_Source()
            {
                return default(AutomaticallyCloneable);
            }

            object ICloneable.Clone()
            {
                return Clone();
            }
        }

        [DeepClone]
        private class Derived : AutomaticallyCloneable
        {
            private string d;

            public override Derived Clone()
            {
                var clone = (Derived)base.Clone();
                clone.d = (string)((ICloneable)this.d).Clone();
                return clone;
            }
        }
    }
