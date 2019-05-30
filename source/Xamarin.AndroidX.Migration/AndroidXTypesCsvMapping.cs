﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Xamarin.AndroidX.Migration
{
	public class AndroidXTypesCsvMapping : CsvMapping
	{
		private const string MappingResourcePath = "Tools/Mappings/androidx-mapping.csv";

		private readonly SortedDictionary<string, FullType> mapping = new SortedDictionary<string, FullType>();
		private readonly SortedDictionary<string, FullType> reverseMapping = new SortedDictionary<string, FullType>();
		private readonly SortedDictionary<string, FullType> javaMapping = new SortedDictionary<string, FullType>();

		public AndroidXTypesCsvMapping()
		{
			var assembly = typeof(AndroidXTypesCsvMapping).Assembly;
			var mappingFile = Path.Combine(Path.GetDirectoryName(assembly.Location), MappingResourcePath);
			LoadMapping(mappingFile);
		}

		public AndroidXTypesCsvMapping(string mappingFile)
			: base(mappingFile)
		{
		}

		public AndroidXTypesCsvMapping(Stream csv)
			: base(csv)
		{
		}

		protected override void OnLoadRecord(string[] record)
		{
			if (record.Length < (int)Column.Messages)
				return;

			var supportNamespace = record[(int)Column.SupportNetNamespace];
			var supportType = record[(int)Column.SupportNetType];
			var xNamespace = record[(int)Column.AndroidXNetNamespace];
			var xType = record[(int)Column.AndroidXNetType];

			var xAssembly = record[(int)Column.AndroidXNetAssembly];

			var supportPackage = record[(int)Column.SupportJavaPackage];
			var supportClass = record[(int)Column.SupportJavaClass];
			var xPackage = record[(int)Column.AndroidXJavaPackage];
			var xClass = record[(int)Column.AndroidXJavaClass];

			if (string.IsNullOrWhiteSpace(supportNamespace) ||
				string.IsNullOrWhiteSpace(supportType) ||
				string.IsNullOrWhiteSpace(xNamespace) ||
				string.IsNullOrWhiteSpace(xType) ||
				string.IsNullOrWhiteSpace(xAssembly) ||
				string.IsNullOrWhiteSpace(supportPackage) ||
				string.IsNullOrWhiteSpace(supportClass) ||
				string.IsNullOrWhiteSpace(xPackage) ||
				string.IsNullOrWhiteSpace(xClass))
				return;

			var support = new FullType(supportNamespace, supportType);
			var androidX = new FullType(xAssembly, xNamespace, xType);
			var supportJava = new FullType(supportPackage, supportClass);
			var androidXJava = new FullType(xPackage, xClass);

			mapping[support.FullName] = androidX;
			reverseMapping[androidX.FullName] = support;
			javaMapping[supportJava.JavaFullName] = androidXJava;
		}

		public bool TryGetAndroidXType(string supportFullName, out FullType androidxType) =>
			mapping.TryGetValue(supportFullName, out androidxType);

		public bool TryGetAndroidXClass(string supportJavaFullName, out FullType androidxClass) =>
			javaMapping.TryGetValue(supportJavaFullName, out androidxClass);

		public bool TryGetSupportType(string androidxFullName, out FullType supportType) =>
			reverseMapping.TryGetValue(androidxFullName, out supportType);

		public bool ContainsSupportType(string supportFullName) =>
			mapping.ContainsKey(supportFullName);

		public bool ContainsSupportClass(string supportJavaFullName) =>
			javaMapping.ContainsKey(supportJavaFullName);

		public bool ContainsAndroidXType(string androidxFullName) =>
			reverseMapping.ContainsKey(androidxFullName);

		public enum Column
		{
			SupportNetNamespace,
			SupportNetType,
			AndroidXNetNamespace,
			AndroidXNetType,
			SupportNetAssembly,
			AndroidXNetAssembly,
			SupportJavaPackage,
			SupportJavaClass,
			AndroidXJavaPackage,
			AndroidXJavaClass,
			Messages
		}
	}
}