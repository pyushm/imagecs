using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace CodeEditor
{
    public class CodeViewHelper : List<Decoration>
    {
        public string Name;
        public static CodeViewHelper None { get { CodeViewHelper ds = new CodeViewHelper(); ds.Name = "None"; return ds; } }
        public static CodeViewHelper Shader
        {   // shader easy-to-read decorations
            get
            {
                CodeViewHelper ds = new CodeViewHelper();
                ds.Name = "Shader";
                ds.Add(new ItemsDecoration(new List<string> { "return", "register" }, Brushes.Blue));
                ds.Add(new ItemsDecoration(new List<string> { "int", "float", "float2", "float3", "float4" }, Brushes.MediumBlue)); // types
                ds.Add(new Decoration(TextParser.QoutedString, Brushes.Crimson)); // quotedText
                ds.Add(new Decoration(TextParser.CppComment, Brushes.Green)); // single line comments
                ds.Add(new Decoration(TextParser.CComment, Brushes.Green)); // multiline comments
                return ds;
            }
        }
        public static CodeViewHelper CSharp3
        {
            get
            {
                CodeViewHelper ds = new CodeViewHelper();
                ds.Name = "C#";
                List<string> reservedWords = new List<string>{ "delegate", "using" ,"namespace" , "static", "class" ,"public" ,"get" , "private" ,
                    "return" , "partial" , "new" ,"set" , "value"  };
                ds.Add(new ItemsDecoration(reservedWords, Brushes.Blue));
                ds.Add(new ItemsDecoration(new List<string> { "string", "int", "double", "long", "float" }, Brushes.BlueViolet)); // types
                ds.Add(new StringDecoration(new List<string> { "#region", "#endregion" }, Brushes.Gray));
                ds.Add(new Decoration(TextParser.QoutedString, Brushes.RosyBrown)); // quotedText
                ds.Add(new Decoration(TextParser.CComment, Brushes.Green)); // multiline comments
                ds.Add(new Decoration(TextParser.CppComment, Brushes.Green)); // single line comments
                return ds;
            }
        }
        #region SQL Server
        public static CodeViewHelper SQLServer2008
        {
            get
            {
                CodeViewHelper ds = new CodeViewHelper();
                ds.Name = "SQL Server";

                // Color Built in functions Magenta
                ItemsDecoration builtInFunctions = new ItemsDecoration();
                builtInFunctions.Brush = new SolidColorBrush(Colors.Magenta);
                builtInFunctions.Items.AddRange(GetBuiltInFunctions());
                ds.Add(builtInFunctions);

                //Color global variables Magenta
                StringDecoration globals = new StringDecoration();
                globals.Brush = new SolidColorBrush(Colors.Magenta);
                globals.Items.AddRange(GetGlobalVariables());
                ds.Add(globals);

                //Color most reserved words blue
                ItemsDecoration bluekeyWords = new ItemsDecoration();
                bluekeyWords.Brush = new SolidColorBrush(Colors.Blue);
                bluekeyWords.Items.AddRange(GetBlueKeyWords());
                ds.Add(bluekeyWords);

                ItemsDecoration grayKeyWords = new ItemsDecoration();
                grayKeyWords.Brush = new SolidColorBrush(Colors.Gray);
                grayKeyWords.Items.AddRange(GetGrayKeyWords());
                ds.Add(grayKeyWords);

                ItemsDecoration dataTypes = new ItemsDecoration();
                dataTypes.Brush = new SolidColorBrush(Colors.Blue);
                dataTypes.Items.AddRange(GetDataTypes());
                ds.Add(dataTypes);


                ItemsDecoration systemViews = new ItemsDecoration();
                systemViews.Brush = new SolidColorBrush(Colors.Green);
                systemViews.Items.AddRange(GetSystemViews());
                ds.Add(systemViews);

                StringDecoration operators = new StringDecoration();
                operators.Brush = new SolidColorBrush(Colors.Gray);
                operators.Items.AddRange(GetOperators());
                ds.Add(operators);


                RegexDecoration quotedText = new RegexDecoration();
                quotedText.Brush = new SolidColorBrush(Colors.Red);
                quotedText.RegexString = "'.*?'";
                ds.Add(quotedText);

                RegexDecoration nQuote = new RegexDecoration();
                //nQuote.DecorationType = EDecorationType.TextColor;
                nQuote.Brush = new SolidColorBrush(Colors.Red);
                nQuote.RegexString = "N''";
                ds.Add(nQuote);


                //Color single line comments green
                RegexDecoration singleLineComment = new RegexDecoration();
                singleLineComment.DecorationType = EDecorationType.TextColor;
                singleLineComment.Brush = new SolidColorBrush(Colors.Green);
                singleLineComment.RegexString = "--.*";
                ds.Add(singleLineComment);

                //Color multiline comments green
                RegexDecoration multiLineComment = new RegexDecoration();
                multiLineComment.DecorationType = EDecorationType.Strikethrough;
                multiLineComment.Brush = new SolidColorBrush(Colors.Green);
                multiLineComment.RegexString = @"(?s:/\*.*?\*/)";
                ds.Add(multiLineComment);
                return ds;
            }
        }



        static string[] GetBuiltInFunctions()
        {
            string[] funct = { "parsename", "db_name", "object_id", "count", "ColumnProperty", "LEN",
                             "CHARINDEX" ,"isnull" , "SUBSTRING" };
            return funct;

        }

        static string[] GetGlobalVariables()
        {

            string[] globals = { "@@fetch_status" };
            return globals;

        }

        static string[] GetDataTypes()
        {
            string[] dt = { "int", "sysname", "nvarchar", "char" };
            return dt;

        }


        static string[] GetBlueKeyWords() // List from 
        {
            string[] res = {"ADD","EXISTS","PRECISION","ALL","EXIT","PRIMARY","ALTER","EXTERNAL",
                            "PRINT","FETCH","PROC","ANY","FILE","PROCEDURE","AS","FILLFACTOR",
                            "PUBLIC","ASC","FOR","RAISERROR","AUTHORIZATION","FOREIGN","READ","BACKUP",
                            "FREETEXT","READTEXT","BEGIN","FREETEXTTABLE","RECONFIGURE","BETWEEN","FROM",
                            "REFERENCES","BREAK","FULL","REPLICATION","BROWSE","FUNCTION","RESTORE",
                            "BULK","GOTO","RESTRICT","BY","GRANT","RETURN","CASCADE","GROUP","REVERT",
                            "CASE","HAVING","REVOKE","CHECK","HOLDLOCK","RIGHT","CHECKPOINT","IDENTITY",
                            "ROLLBACK","CLOSE","IDENTITY_INSERT","ROWCOUNT","CLUSTERED","IDENTITYCOL",
                            "ROWGUIDCOL","COALESCE","IF","RULE","COLLATE","IN","SAVE","COLUMN","INDEX",
                            "SCHEMA","COMMIT","INNER","SECURITYAUDIT","COMPUTE","INSERT","SELECT",
                            "CONSTRAINT","INTERSECT","SESSION_USER","CONTAINS","INTO","SET","CONTAINSTABLE",
                            "SETUSER","CONTINUE","JOIN","SHUTDOWN","CONVERT","KEY","SOME","CREATE",
                            "KILL","STATISTICS","CROSS","LEFT","SYSTEM_USER","CURRENT","LIKE","TABLE",
                            "CURRENT_DATE","LINENO","TABLESAMPLE","CURRENT_TIME","LOAD","TEXTSIZE",
                            "CURRENT_TIMESTAMP","MERGE","THEN","CURRENT_USER","NATIONAL","TO","CURSOR",
                            "NOCHECK","TOP","DATABASE","NONCLUSTERED","TRAN","DBCC","NOT","TRANSACTION",
                            "DEALLOCATE","NULL","TRIGGER","DECLARE","NULLIF","TRUNCATE","DEFAULT","OF",
                            "TSEQUAL","DELETE","OFF","UNION","DENY","OFFSETS","UNIQUE","DESC", "ON",
                            "UNPIVOT","DISK","OPEN","UPDATE","DISTINCT","OPENDATASOURCE","UPDATETEXT",
                            "DISTRIBUTED","OPENQUERY","USE","DOUBLE","OPENROWSET","USER","DROP","OPENXML",
                            "VALUES","DUMP","OPTION","VARYING","ELSE","OR","VIEW","END","ORDER","WAITFOR",
                            "ERRLVL","OUTER","WHEN","ESCAPE","OVER","WHERE","EXCEPT","PERCENT","WHILE",
                            "EXEC","PIVOT","WITH","EXECUTE","PLAN","WRITETEXT", "GO", "ANSI_NULLS",
                            "NOCOUNT", "QUOTED_IDENTIFIER", "master"};

            return res;
        }


        static string[] GetGrayKeyWords()
        {
            string[] res = { "AND", "Null", "IS" };

            return res;

        }

        static string[] GetOperators()
        {
            string[] ops = { "=", "+", ".", ",", "-", "(", ")", "*", "<", ">" };

            return ops;

        }

        static string[] GetSystemViews()
        {
            string[] views = { "syscomments", "sysobjects", "sys.syscomments" };
            return views;
        }

        #endregion
        #region DBML

        public static CodeViewHelper Dbml
        {
            get
            {
                CodeViewHelper ds = new CodeViewHelper();

                ds.Name = "Dbml#";

                ItemsDecoration BrownWords = new ItemsDecoration();
                BrownWords.Brush = new SolidColorBrush(Colors.Brown);
                BrownWords.Items = new List<string>() { "xml", "Database", "Table", "Type", "Column", "Association" };
                ds.Add(BrownWords);


                ItemsDecoration RedWords = new ItemsDecoration();
                RedWords.Brush = new SolidColorBrush(Colors.Red);
                RedWords.Items = new List<string>() { "version", "encoding", "Name", "Class", "xmlns", "Member", "Type", "DbType", "CanBeNull" , "DeleteRule", "IsPrimaryKey"
              ,"IsForeignKey", "ThisKey", "OtherKey", "IsDbGenerated" ,"UpdateCheck" };
                ds.Add(RedWords);

                RegexDecoration quoted = new RegexDecoration("\".*?\"", Brushes.Blue);
                ds.Add(quoted);


                StringDecoration blueStrings = new StringDecoration();
                blueStrings.Brush = new SolidColorBrush(Colors.Blue);
                blueStrings.Items = new List<string>() { "<", "?", "=", "/", ">" };
                ds.Add(blueStrings);


                StringDecoration quotationMarks = new StringDecoration("\"", Brushes.Black);
                ds.Add(quotationMarks);
                return ds;
            }
        }
        #endregion
        #region XML
        public static CodeViewHelper Xml
        {
            get
            {
                CodeViewHelper ds = new CodeViewHelper();
                ds.Name = "XML";

                StringDecoration specialCharacters = new StringDecoration();
                specialCharacters.Brush = new SolidColorBrush(Colors.Blue);
                specialCharacters.Items = new List<string>() { "<", "/", ">", "<?", "?>", "=" };
                ds.Add(specialCharacters);

                RegexMatchDecoration xmlTagName = new RegexMatchDecoration();
                xmlTagName.Brush = new SolidColorBrush(Colors.Brown);
                xmlTagName.RegexString = @"</?(?<selected>\w.*?)(\s|>|/)";
                ds.Add(xmlTagName);

                RegexMatchDecoration xmlAttributeName = new RegexMatchDecoration();
                xmlAttributeName.Brush = new SolidColorBrush(Colors.Red);
                xmlAttributeName.RegexString = @"\s(?<selected>\w+|\w+:\w+|(\w|\.)+)="".*?""";
                ds.Add(xmlAttributeName);

                RegexMatchDecoration xmlAttributeValue = new RegexMatchDecoration();
                xmlAttributeValue.Brush = new SolidColorBrush(Colors.Blue);
                xmlAttributeValue.RegexString = @"\s(\w+|\w+:\w+|(\w|\.)+)\s*=\s*""(?<selected>.*?)""";
                ds.Add(xmlAttributeValue);

                RegexMatchDecoration xml = new RegexMatchDecoration();
                xml.Brush = new SolidColorBrush(Colors.Brown);
                xml.RegexString = @"<\?(?<selected>xml)";
                ds.Add(xml);

                return ds;
            }
        }
        #endregion
        #region XAML
        public static CodeViewHelper Xaml
        {
            get
            {
                CodeViewHelper ds = new CodeViewHelper();
                ds.Name = "XAML";
                StringDecoration specialCharacters = new StringDecoration();
                specialCharacters.Brush = new SolidColorBrush(Colors.Blue);
                specialCharacters.Items = new List<string>() { "<", "/", ">", "<?", "?>" };
                ds.Add(specialCharacters);

                RegexMatchDecoration xmlTagName = new RegexMatchDecoration();
                xmlTagName.Brush = new SolidColorBrush(Colors.Brown);
                xmlTagName.RegexString = @"</?(?<selected>\w.*?)(\s|>|/)";
                ds.Add(xmlTagName);

                RegexMatchDecoration xmlAttributeName = new RegexMatchDecoration();
                xmlAttributeName.Brush = new SolidColorBrush(Colors.Red);
                xmlAttributeName.RegexString = @"\s(?<selected>\w+|\w+:\w+|(\w|\.)+)="".*?""";
                ds.Add(xmlAttributeName);

                RegexMatchDecoration xmlAttributeValue = new RegexMatchDecoration();
                xmlAttributeValue.Brush = new SolidColorBrush(Colors.Blue);
                xmlAttributeValue.RegexString = @"\s(\w+|\w+:\w+|(\w|\.)+)\s*(?<selected>=\s*"".*?"")";
                ds.Add(xmlAttributeValue);

                RegexMatchDecoration xamlMarkupExtension = new RegexMatchDecoration();
                xamlMarkupExtension.RegexString = @"\s(\w+|\w+:\w+|(\w|\.)+)\s*=\s*""{(?<selected>.*?)\s+.*?}""";
                xamlMarkupExtension.Brush = new SolidColorBrush(Colors.Brown);
                ds.Add(xamlMarkupExtension);

                RegexMatchDecoration xamlMarkupExtensionValue = new RegexMatchDecoration();
                xamlMarkupExtensionValue.RegexString = @"\s(\w+|\w+:\w+|(\w|\.)+)\s*=\s*""{.*?\s+(?<selected>.*?)}""";
                xamlMarkupExtensionValue.Brush = new SolidColorBrush(Colors.Red);
                ds.Add(xamlMarkupExtensionValue);

                DoubleRegexDecoration MarkupPeriods = new DoubleRegexDecoration();
                MarkupPeriods.Brush = new SolidColorBrush(Colors.Blue);
                MarkupPeriods.OuterRegexString = @"\s(\w+|\w+:\w+|(\w|\.)+)\s*=\s*""{.*?\s+.*?}""";
                MarkupPeriods.InnerRegexString = @"\.";
                ds.Add(MarkupPeriods);

                RegexMatchDecoration xml = new RegexMatchDecoration();
                xml.Brush = new SolidColorBrush(Colors.Brown);
                xml.RegexString = @"<\?(?<selected>xml)";
                ds.Add(xml);

                RegexMatchDecoration elementValues = new RegexMatchDecoration();
                elementValues.Brush = new SolidColorBrush(Colors.Brown);
                elementValues.RegexString = @">(?<selected>.*?)</";
                ds.Add(elementValues);

                RegexDecoration comment = new RegexDecoration();
                comment.Brush = new SolidColorBrush(Colors.Green);
                comment.RegexString = @"<!--.*?-->";
                ds.Add(comment);

                return ds;
            }
        }

        #endregion
    }
}
