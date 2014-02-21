namespace RevaleeService
{
	internal interface IPartialMatchDictionary<KeyType, ValueType>
	{
		ValueType Match(KeyType key);
	}
}