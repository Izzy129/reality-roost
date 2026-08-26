using System;

[Flags]
public enum ContentType
{
    None = 0,
    Model = 1 << 0,
    Video = 1 << 1,
    Slideshow = 1 << 2
}
