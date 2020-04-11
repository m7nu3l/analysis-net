using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRM = System.Reflection.Metadata;

namespace MetadataProvider
{
	internal static class TypeHelper
	{
		public static IType ToType(SRM.PrimitiveTypeCode typeCode)
		{
			switch (typeCode)
			{
				case SRM.PrimitiveTypeCode.Boolean: return PlatformType.Boolean;
				case SRM.PrimitiveTypeCode.Byte: return PlatformType.Byte;
				case SRM.PrimitiveTypeCode.Char: return PlatformType.Char;
				case SRM.PrimitiveTypeCode.Double: return PlatformType.Double;
				case SRM.PrimitiveTypeCode.Int16: return PlatformType.Int16;
				case SRM.PrimitiveTypeCode.Int32: return PlatformType.Int32;
				case SRM.PrimitiveTypeCode.Int64: return PlatformType.Int64;
				case SRM.PrimitiveTypeCode.IntPtr: return PlatformType.IntPtr;
				case SRM.PrimitiveTypeCode.Object: return PlatformType.Object;
				case SRM.PrimitiveTypeCode.SByte: return PlatformType.SByte;
				case SRM.PrimitiveTypeCode.Single: return PlatformType.Single;
				case SRM.PrimitiveTypeCode.String: return PlatformType.String;
				case SRM.PrimitiveTypeCode.UInt16: return PlatformType.UInt16;
				case SRM.PrimitiveTypeCode.UInt32: return PlatformType.UInt32;
				case SRM.PrimitiveTypeCode.UInt64: return PlatformType.UInt64;
				case SRM.PrimitiveTypeCode.UIntPtr: return PlatformType.UIntPtr;
				case SRM.PrimitiveTypeCode.Void: return PlatformType.Void;

				//case SRM.PrimitiveTypeCode.TypedReference:	return "typedref";

				default: throw typeCode.ToUnknownValueException();
			}
		}

		public static IType ToType(SRM.ConstantTypeCode typeCode)
		{
			switch (typeCode)
			{
				case SRM.ConstantTypeCode.Boolean: return PlatformType.Boolean;
				case SRM.ConstantTypeCode.Byte: return PlatformType.Byte;
				case SRM.ConstantTypeCode.Char: return PlatformType.Char;
				case SRM.ConstantTypeCode.Double: return PlatformType.Double;
				case SRM.ConstantTypeCode.Int16: return PlatformType.Int16;
				case SRM.ConstantTypeCode.Int32: return PlatformType.Int32;
				case SRM.ConstantTypeCode.Int64: return PlatformType.Int64;
				case SRM.ConstantTypeCode.SByte: return PlatformType.SByte;
				case SRM.ConstantTypeCode.Single: return PlatformType.Single;
				case SRM.ConstantTypeCode.String: return PlatformType.String;
				case SRM.ConstantTypeCode.UInt16: return PlatformType.UInt16;
				case SRM.ConstantTypeCode.UInt32: return PlatformType.UInt32;
				case SRM.ConstantTypeCode.UInt64: return PlatformType.UInt64;
				case SRM.ConstantTypeCode.NullReference: return PlatformType.Object;

				default: throw typeCode.ToUnknownValueException();
			}
		}
	}
}
