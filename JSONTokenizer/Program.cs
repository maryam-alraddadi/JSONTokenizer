using System;
using System.Collections.Generic;
using System.Dynamic;

namespace JSONTokenizer
{
    public delegate bool InputCondition(Input input);
    public class Input
    {
        private readonly string input;
        private readonly int length;
        private int position;
        private int lineNumber;
        //Properties
        public int Length
        {
            get
            {
                return this.length;
            }
        }
        public int Position
        {
            get
            {
                return this.position;
            }
        }
        public int NextPosition
        {
            get
            {
                return this.position + 1;
            }
        }
        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }
        }
        public char Character
        {
            get
            {
                if (this.position > -1) return this.input[this.position];
                else return '\0';
            }
        }
        public Input(string input)
        {
            this.input = input;
            this.length = input.Length;
            this.position = -1;
            this.lineNumber = 1;
        }
        public bool hasMore(int numOfSteps = 1)
        {
            if (numOfSteps <= 0) throw new Exception("Invalid number of steps");
            return (this.position + numOfSteps) < this.length;
        }
        public bool hasLess(int numOfSteps = 1)
        {
            if (numOfSteps <= 0) throw new Exception("Invalid number of steps");
            return (this.position - numOfSteps) > -1;
        }
        public Input step(int numOfSteps = 1)
        {
            if (this.hasMore(numOfSteps))
                this.position += numOfSteps;
            else
            {
                throw new Exception("There is no more step");
            }
            return this;
        }
        public Input back(int numOfSteps = 1)
        {
            if (this.hasLess(numOfSteps))
                this.position -= numOfSteps;
            else
            {
                throw new Exception("There is no more step");
            }
            return this;
        }
        public Input reset() { return this; }
        public char peek(int numOfSteps = 1) {
            if (this.hasMore(numOfSteps)) return this.input[this.Position + numOfSteps];
            return '\0';
        }
        
        public char peekBack(int numOfSteps = 1)
        {
            if (this.hasLess(numOfSteps))
            {
                return this.input[this.Position + 1 - numOfSteps];
            }

            return '\0';
            //throw new Exception("There is no more step");
        }
        public string loop(InputCondition condition) {
            string buffer = "";
            while (this.hasMore() && condition(this))
                buffer += this.step().Character;
            return buffer;
        }
    }
    public class Token {
        public int Position { set; get; }
        public int LineNumber { set; get; }
        public string Type { set; get; }
        public string Value { set; get; }
        public Token(int position, int lineNumber, string type, string value) {
            this.Position = position;
            this.LineNumber = lineNumber;
            this.Type = type;
            this.Value = value;
        }
    }
    public abstract class Tokenizable
    {
        public abstract bool tokenizable(Tokenizer tokenizer);
        public abstract Token tokenize(Tokenizer tokenizer);
    }
    public class Tokenizer {
        //public List<Token> tokens;
        public Input input;
        public Tokenizable[] handlers;
        public Tokenizer(string source, Tokenizable[] handlers) {
            this.input = new Input(source);
            this.handlers = handlers;
        }
        public Tokenizer(Input source, Tokenizable[] handlers) {
            this.input = source;
            this.handlers = handlers;
        }
        public Token tokenize() {
            
            foreach (var handler in this.handlers)
                if (handler.tokenizable(this)) return handler.tokenize(this);
            return null;
        }
    }
    public class IdTokenizer : Tokenizable
    {
        private List<string> keywords;
        public IdTokenizer(List<string> keywords)
        {
            this.keywords = keywords;
        }
        public override bool tokenizable(Tokenizer t)
        {
            char currentCharacter = t.input.peek();
            //Console.WriteLine(currentCharacter);
            return Char.IsLetter(currentCharacter) || currentCharacter == '_';
        }
        static bool isId(Input input)
        {
            char currentCharacter = input.peek();
            return Char.IsLetterOrDigit(currentCharacter) || currentCharacter == '_';
        }
        public override Token tokenize(Tokenizer t)
        { 
            return new Token(t.input.Position, t.input.LineNumber,
                "identifier", t.input.loop(isId));
        }
    }

    public class IsWhiteSpace : Tokenizable
    {
        public override bool tokenizable(Tokenizer t)
        {
            char currentCharacter = t.input.peek();
            return Char.IsWhiteSpace(currentCharacter);
        }

        static bool IsSpace(Input input)
        {
            char currentCharacter = input.peek();
            return Char.IsWhiteSpace(currentCharacter);
        }

        static bool IsLineFeed(Input input)
        {
            char currentCharacter = input.peek();
            if (currentCharacter.Equals('\n')) return true;
            else return false;
        }

        static bool carriageReutrn(Input input)
        {
            char currentCharacter = input.peek();
            if (currentCharacter.Equals('\r')) return true;
            else return false;
        }

        static bool HorizentlTab(Input input)
        {
            char currentCharacter = input.peek();
            if (currentCharacter.Equals('\t')) return true;
            else return false;
        }

        public override Token tokenize(Tokenizer t)
        {
            Token token = new Token(t.input.Position, t.input.LineNumber,
                "whitespppppppace", t.input.loop(IsSpace));
            InputCondition[] i = {IsSpace, HorizentlTab, carriageReutrn, IsLineFeed};
            foreach (var conditon in i)
            {
                token.Value += t.input.loop(conditon);
            }

            return token;
        }
    }


    public class NumberTokenizer : Tokenizable
    {
        public override bool tokenizable(Tokenizer t)
        {
            char currentCharacter = t.input.peek();
            char nextCharacter = t.input.peek(2);
            return IsOneNineDigit(currentCharacter) || 
                   (currentCharacter == '0' && nextCharacter =='.') ||
                   (currentCharacter == '-' && Char.IsDigit(nextCharacter));
        }

        public override Token tokenize(Tokenizer t)
        {
            InputCondition[] i = {IsDigit, IsFraction, IsNegative, IsExponent, IsExponentSigned};
            Token token = new Token(t.input.Position, t.input.LineNumber,
                "number", "");
            int k = 0;
            while (k < t.input.Length)
            {
                foreach (var condition in i)
                {
                    token.Value += t.input.loop(condition);
                    //Console.WriteLine(token.Value);
                    //if value has more than one . e + - throw exception
                }
                k++;
            }
            
            return token;
        }

        static bool IsOneNineDigit(char character)
        {
            return (character >= '1' && character <= '9');
        }

        static bool IsDigit(Input input)
        {
            return Char.IsDigit(input.peek());
        }

        static bool IsFraction(Input input)
        {
            return input.peek() == '.' && Char.IsDigit(input.peek(2));
        }

        static bool IsNegative(Input input)
        {
            return input.peek() == '-' && Char.IsDigit(input.peek(2));
        }

        static bool IsExponent(Input input)
        {
            char currentCharacter = input.peek();
            char nextCharacter = input.peek(2);
            return (currentCharacter == 'e' || currentCharacter == 'E') &&
                   (Char.IsDigit(nextCharacter) || nextCharacter == '+' ||
                    nextCharacter == '-');
        }

        static bool IsExponentSigned(Input input)
        {
            char currentCharacter = input.peek();
            char nextCharacter = input.peek(2);
            char previousCharacter = input.peekBack();
            return ((currentCharacter == '+' || currentCharacter == '-') &&
                    (previousCharacter == 'e' || previousCharacter == 'E')
                    && Char.IsDigit(input.peek(2)));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Tokenizer t = new Tokenizer(new Input("-4353 033 1.1 0.43 55.6E-2 1.33e99 34543.45 1.4342 114.1"), new Tokenizable[] {
                new IsWhiteSpace(),
                new NumberTokenizer()
            }); ;
            Token token = t.tokenize();
            while (token != null)
            {
                Console.WriteLine($"value: {token.Value}            type: {token.Type}");
                token = t.tokenize();
            }
        }
    }
}