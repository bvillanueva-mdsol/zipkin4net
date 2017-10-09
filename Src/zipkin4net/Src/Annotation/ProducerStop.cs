﻿namespace zipkin4net.Annotation
{
    public sealed class ProducerStop : IAnnotation
    {
        internal ProducerStop()
        {}

        public override string ToString()
        {
            return GetType().Name;
        }

        public void Accept(IAnnotationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
