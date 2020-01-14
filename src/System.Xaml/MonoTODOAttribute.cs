using System;

class MonoTODOAttribute : Attribute
{
	public string Message { get; private set; }

	public MonoTODOAttribute (string message = null)
	{
		Message = message;
	}
}

