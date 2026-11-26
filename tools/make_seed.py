"""Generates the embedded seed JSON files. Expected outputs for DSA problems are computed by
reference solutions below, so the test cases are verified rather than hand-typed."""
import json, os
OUT = os.path.join(os.path.dirname(__file__), "..", "src", "PrepPilot.Infrastructure", "Seed")

# ------------------------------------------------------------------ question pool
def q(text, options, answer, explanation):
    return {"text": text, "options": options, "answer": answer, "explanation": explanation}

pool = {}
pool["Quantitative Aptitude"] = [
    q("A shopkeeper sells an article at a 20% profit. If the cost price is ₹500, what is the selling price?",
      ["₹550", "₹600", "₹620", "₹650"], 1, "SP = 500 × 1.20 = ₹600."),
    q("A train 150 m long passes a pole in 15 seconds. What is its speed in km/h?",
      ["30", "36", "40", "45"], 1, "Speed = 150/15 = 10 m/s = 10 × 18/5 = 36 km/h."),
    q("A can finish a job in 12 days and B in 18 days. Working together, how many days will they take?",
      ["6", "7.2", "8", "7.5"], 1, "Combined rate = 1/12 + 1/18 = 5/36 per day, so time = 36/5 = 7.2 days."),
    q("The average of five numbers is 24. If one number is removed, the average of the remaining four becomes 22. What number was removed?",
      ["28", "30", "32", "34"], 2, "Total = 120, remaining total = 88, so the removed number is 120 − 88 = 32."),
    q("What is the simple interest on ₹8,000 at 5% per annum for 3 years?",
      ["₹1,000", "₹1,200", "₹1,500", "₹1,600"], 1, "SI = P × R × T / 100 = 8000 × 5 × 3 / 100 = ₹1,200."),
    q("Two numbers are in the ratio 3:5 and their sum is 96. What is the larger number?",
      ["36", "48", "60", "64"], 2, "Larger = 96 × 5/8 = 60."),
    q("The price of an item rises from ₹80 to ₹100. What is the percentage increase?",
      ["20%", "25%", "30%", "15%"], 1, "Increase = 20 on a base of 80, so 20/80 × 100 = 25%."),
    q("A boat covers 24 km downstream in 2 hours and returns upstream in 3 hours. What is the speed of the current?",
      ["1 km/h", "2 km/h", "3 km/h", "4 km/h"], 1, "Downstream = 12 km/h, upstream = 8 km/h. Current = (12 − 8)/2 = 2 km/h."),
]
pool["Logical Reasoning"] = [
    q("Find the next number: 2, 6, 12, 20, 30, ?", ["40", "42", "44", "46"], 1,
      "Differences are 4, 6, 8, 10, so the next difference is 12 and the number is 42."),
    q("Find the odd one out: 5, 7, 9, 11", ["5", "7", "9", "11"], 2, "5, 7 and 11 are prime; 9 is not."),
    q("If CAT is coded as DBU, how is DOG coded?", ["EPH", "DPH", "EOH", "FPH"], 0,
      "Each letter moves one step forward: D→E, O→P, G→H."),
    q("Pointing to a photograph, Riya says, 'He is the son of my mother's only brother.' How is the man related to Riya?",
      ["Brother", "Cousin", "Uncle", "Nephew"], 1, "Mother's brother's son is Riya's maternal cousin."),
    q("A man walks 5 km north, then 3 km east, then 5 km south. How far is he from the starting point?",
      ["3 km", "5 km", "8 km", "13 km"], 0, "North and south cancel out, leaving 3 km east."),
    q("Statements: All engineers are graduates. Some graduates are artists. Conclusions: I. Some engineers are artists. II. All engineers are graduates.",
      ["Only I follows", "Only II follows", "Both follow", "Neither follows"], 1,
      "II is given directly. I does not necessarily follow, because the graduates who are artists may not be engineers."),
    q("Find the missing number: 1, 4, 9, 16, ?, 36", ["20", "24", "25", "30"], 2, "These are squares: 1², 2², 3², 4², 5² = 25, 6² = 36."),
    q("What is the angle between the hands of a clock at 3:00?", ["60°", "90°", "120°", "180°"], 1,
      "The hour hand is at 3 and the minute hand at 12, which is 3 × 30° = 90°."),
]
pool["Verbal Ability"] = [
    q("Choose the synonym of 'Abundant'.", ["Scarce", "Plentiful", "Rare", "Meagre"], 1, "Abundant means existing in large quantities, so plentiful."),
    q("Choose the antonym of 'Benevolent'.", ["Kind", "Generous", "Malevolent", "Charitable"], 2, "Benevolent means well-meaning and kind; malevolent means wishing harm."),
    q("Choose the grammatically correct sentence.",
      ["She don't like coffee.", "She doesn't likes coffee.", "She doesn't like coffee.", "She not like coffee."], 2,
      "With a third-person singular subject, use 'doesn't' followed by the base verb 'like'."),
    q("One word for 'a person who can speak two languages fluently':", ["Bilingual", "Polyglot", "Linguist", "Interpreter"], 0,
      "Bilingual means two languages. A polyglot knows many."),
    q("Fill in the blank: He has been working here ____ 2019.", ["for", "since", "from", "during"], 1,
      "'Since' is used with a point in time (2019); 'for' is used with a duration."),
    q("'To break the ice' means:",
      ["To start a conversation in an awkward situation", "To damage something", "To feel very cold", "To end a friendship"], 0,
      "It is an idiom for easing initial social tension."),
    q("Choose the correctly spelled word.", ["Accomodate", "Accommodate", "Acommodate", "Accomodete"], 1, "Accommodate has double 'c' and double 'm'."),
]
pool["Computer Science"] = [
    q("Which data structure follows LIFO order?", ["Queue", "Stack", "Array", "Heap"], 1, "A stack removes the most recently added element first."),
    q("What is the time complexity of binary search on a sorted array of n elements?", ["O(n)", "O(log n)", "O(n log n)", "O(1)"], 1, "Each step halves the search space."),
    q("Which normal form removes partial dependencies?", ["1NF", "2NF", "3NF", "BCNF"], 1, "2NF requires that no non-key attribute depends on part of a composite key."),
    q("Which CPU scheduling algorithm can cause starvation of long processes?", ["FCFS", "Round Robin", "Shortest Job First", "None of these"], 2,
      "If short jobs keep arriving, long jobs may wait indefinitely under SJF."),
    q("Which layer of the OSI model is responsible for routing packets?", ["Data Link", "Network", "Transport", "Session"], 1, "The Network layer (IP) handles logical addressing and routing."),
    q("Which OOP concept lets the same method call behave differently depending on the object's actual class?",
      ["Encapsulation", "Polymorphism", "Abstraction", "Composition"], 1, "Runtime polymorphism (method overriding) resolves the method by the object's real type."),
    q("What is the worst-case time complexity of QuickSort?", ["O(n log n)", "O(n)", "O(n²)", "O(log n)"], 2, "A consistently bad pivot gives O(n²)."),
    q("Which statement about a primary key is FALSE?",
      ["It uniquely identifies each row", "It cannot be NULL", "It can contain duplicate values", "A table can have only one"], 2, "Primary key values must be unique."),
]
pool["SQL"] = [
    q("Which clause filters groups after GROUP BY?", ["WHERE", "HAVING", "ORDER BY", "LIMIT"], 1, "WHERE filters rows before grouping; HAVING filters groups."),
    q("Which join returns all rows from the left table and the matching rows from the right table?", ["INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "CROSS JOIN"], 1, "LEFT JOIN keeps every left row, using NULLs where there is no match."),
    q("What does SELECT COUNT(*) return?", ["Number of columns", "Number of rows", "Sum of values", "Number of distinct values"], 1, "COUNT(*) counts rows."),
    q("Which keyword removes duplicate rows from a result set?", ["UNIQUE", "DISTINCT", "REMOVE", "DIFFERENT"], 1, "SELECT DISTINCT returns unique rows."),
    q("Which statement quickly removes all rows from a table but keeps its structure?", ["DROP TABLE", "DELETE", "TRUNCATE TABLE", "REMOVE"], 2, "TRUNCATE removes all rows and keeps the table definition."),
    q("A column Marks has the values 10, 20, NULL, 30. What does AVG(Marks) return?", ["15", "20", "30", "NULL"], 1, "AVG ignores NULLs: (10 + 20 + 30) / 3 = 20."),
]
pool["Programming"] = [
    q("What is the output?\n\nx = [1, 2, 3]\ny = x\ny.append(4)\nprint(len(x))", ["3", "4", "1", "Error"], 1, "y and x refer to the same list, so appending through y changes x."),
    q("What is the output of the Python statement?\n\nprint(10 // 3, 10 % 3)", ["3 1", "3.33 1", "3 3", "1 3"], 0, "// is floor division (3) and % is the remainder (1)."),
    q("What is the output?\n\ndef f(n):\n    return 1 if n <= 1 else n * f(n - 1)\nprint(f(5))", ["25", "120", "60", "24"], 1, "f(5) = 5! = 120."),
    q("What is the output?\n\nprint('ab' * 3 + 'c')", ["ababc", "abababc", "ab3c", "Error"], 1, "'ab' * 3 is 'ababab'; adding 'c' gives 'abababc'."),
    q("What is the output?\n\nnums = [1, 2, 3, 4, 5]\nprint(nums[1:4])", ["[1, 2, 3]", "[2, 3, 4]", "[2, 3, 4, 5]", "[1, 2, 3, 4]"], 1, "Slicing [1:4] takes indexes 1, 2 and 3."),
    q("How many times does the loop body run?\n\ncount = 0\nfor (i = 1; i <= 10; i += 3)\n    count++", ["3", "4", "5", "10"], 1, "i takes the values 1, 4, 7, 10, so the body runs 4 times."),
    q("What is the output?\n\nprint(bool([]), bool([0]))", ["False True", "True True", "False False", "True False"], 0, "An empty list is falsy; a non-empty list is truthy even if it contains 0."),
]

# verify the Python snippets and arithmetic claims that can be executed
x = [1, 2, 3]; y = x; y.append(4); assert len(x) == 4
assert (10 // 3, 10 % 3) == (3, 1)
def f(n): return 1 if n <= 1 else n * f(n - 1)
assert f(5) == 120
assert 'ab' * 3 + 'c' == 'abababc'
assert [1, 2, 3, 4, 5][1:4] == [2, 3, 4]
assert len(range(1, 11, 3)) == 4
assert (bool([]), bool([0])) == (False, True)
assert 500 * 1.2 == 600 and 150 / 15 * 18 / 5 == 36 and abs(1/(1/12 + 1/18) - 7.2) < 1e-9
assert 120 - 88 == 32 and 8000 * 5 * 3 // 100 == 1200 and 96 * 5 // 8 == 60 and (100 - 80) / 80 == 0.25
assert (24 / 2 - 24 / 3) / 2 == 2
assert (10 + 20 + 30) / 3 == 20
for topic, items in pool.items():
    for item in items:
        assert 0 <= item["answer"] < len(item["options"]), item["text"]
        assert len(set(item["options"])) == len(item["options"]), item["text"]

json.dump(pool, open(os.path.join(OUT, "questions.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)

# ------------------------------------------------------------------ companies
T = list(pool.keys())
def paper(title, diff, dur, mix, offset, passmark=50):
    for t, n in mix.items():
        assert n <= len(pool[t]), (title, t)
    return {"title": title, "difficulty": diff, "durationMinutes": dur, "passMark": passmark, "offset": offset, "mix": mix}

NOTE = " Patterns change every hiring season, so verify the latest on the official careers page."
companies = [
    {"name": "TCS", "slug": "tcs", "emoji": "🏢", "difficulty": "Easy",
     "description": "India's largest IT services company. Hires in bulk through the TCS NQT." ,
     "pattern": ["Online test (NQT): numerical, verbal and reasoning ability", "Programming logic and coding questions (more for Digital / Prime roles)", "Technical interview, managerial interview and HR interview", "Commonly reported pattern." + NOTE],
     "papers": [paper("TCS NQT Style Mock 1", "Easy", 30, {"Quantitative Aptitude": 6, "Logical Reasoning": 5, "Verbal Ability": 5, "Programming": 4}, 0),
                paper("TCS NQT Style Mock 2", "Medium", 30, {"Quantitative Aptitude": 6, "Logical Reasoning": 5, "Verbal Ability": 4, "Programming": 5}, 2)]},
    {"name": "Infosys", "slug": "infosys", "emoji": "💼", "difficulty": "Medium",
     "description": "Global IT consulting major with online assessments for campus and off-campus drives.",
     "pattern": ["Online assessment: quantitative, logical and verbal ability", "Pseudocode / puzzle-solving and coding questions", "Technical interview followed by HR interview", "Commonly reported pattern." + NOTE],
     "papers": [paper("Infosys Online Assessment Mock 1", "Medium", 35, {"Quantitative Aptitude": 5, "Logical Reasoning": 5, "Verbal Ability": 4, "Programming": 5, "Computer Science": 3}, 0),
                paper("Infosys Online Assessment Mock 2", "Medium", 35, {"Quantitative Aptitude": 5, "Logical Reasoning": 6, "Verbal Ability": 4, "Programming": 4, "Computer Science": 3}, 3)]},
    {"name": "Wipro", "slug": "wipro", "emoji": "🌐", "difficulty": "Easy",
     "description": "IT services and consulting company hiring through the National Level Talent Hunt style tests.",
     "pattern": ["Online aptitude test: quantitative, logical and verbal", "Written communication (essay) section", "Coding section", "Technical and HR interviews", "Commonly reported pattern." + NOTE],
     "papers": [paper("Wipro Aptitude Mock 1", "Easy", 25, {"Quantitative Aptitude": 6, "Logical Reasoning": 4, "Verbal Ability": 5, "Programming": 3}, 0),
                paper("Wipro Aptitude Mock 2", "Medium", 25, {"Quantitative Aptitude": 5, "Logical Reasoning": 5, "Verbal Ability": 5, "Programming": 3}, 2)]},
    {"name": "Accenture", "slug": "accenture", "emoji": "🎯", "difficulty": "Medium",
     "description": "Consulting and technology services firm with a cognitive plus technical assessment.",
     "pattern": ["Cognitive and technical assessment: analytical, quantitative, verbal and abstract reasoning", "Technical MCQs on CS fundamentals, networking, cloud and security", "Coding section", "Communication assessment, then interview", "Commonly reported pattern." + NOTE],
     "papers": [paper("Accenture Cognitive & Technical Mock 1", "Medium", 30, {"Quantitative Aptitude": 4, "Logical Reasoning": 5, "Computer Science": 6, "SQL": 3, "Programming": 3}, 0),
                paper("Accenture Cognitive & Technical Mock 2", "Medium", 30, {"Quantitative Aptitude": 4, "Logical Reasoning": 4, "Computer Science": 6, "SQL": 4, "Programming": 3}, 2)]},
    {"name": "Cognizant", "slug": "cognizant", "emoji": "🔷", "difficulty": "Medium",
     "description": "IT services company that hires freshers through its GenC programs.",
     "pattern": ["Online test: aptitude (quantitative, logical, verbal)", "Technical MCQs and coding", "Interview rounds", "Commonly reported pattern." + NOTE],
     "papers": [paper("Cognizant GenC Style Mock 1", "Medium", 30, {"Quantitative Aptitude": 5, "Logical Reasoning": 5, "Verbal Ability": 4, "Computer Science": 4, "Programming": 3}, 0),
                paper("Cognizant GenC Style Mock 2", "Medium", 30, {"Quantitative Aptitude": 5, "Logical Reasoning": 4, "Verbal Ability": 4, "Computer Science": 5, "Programming": 3}, 3)]},
    {"name": "Capgemini", "slug": "capgemini", "emoji": "🧩", "difficulty": "Medium",
     "description": "Consulting and technology company with an aptitude, pseudocode and communication assessment.",
     "pattern": ["Aptitude and game-based reasoning", "Pseudocode and CS fundamentals", "English communication assessment", "Technical and HR interviews", "Commonly reported pattern." + NOTE],
     "papers": [paper("Capgemini Assessment Mock 1", "Medium", 30, {"Quantitative Aptitude": 4, "Logical Reasoning": 5, "Verbal Ability": 4, "Programming": 5, "Computer Science": 3}, 0),
                paper("Capgemini Assessment Mock 2", "Medium", 30, {"Quantitative Aptitude": 5, "Logical Reasoning": 5, "Verbal Ability": 3, "Programming": 5, "Computer Science": 3}, 2)]},
    {"name": "Deloitte", "slug": "deloitte", "emoji": "🟢", "difficulty": "Medium",
     "description": "Big-four consulting firm hiring analysts and engineers through online assessments and interviews.",
     "pattern": ["Online assessment: aptitude plus technical fundamentals", "Technical interview on projects, SQL and CS basics", "HR or managerial interview", "Commonly reported pattern." + NOTE],
     "papers": [paper("Deloitte Analyst Mock 1", "Medium", 30, {"Quantitative Aptitude": 5, "Logical Reasoning": 4, "Verbal Ability": 4, "Computer Science": 4, "SQL": 3}, 0),
                paper("Deloitte Analyst Mock 2", "Hard", 30, {"Quantitative Aptitude": 5, "Logical Reasoning": 5, "Verbal Ability": 3, "Computer Science": 4, "SQL": 3}, 3)]},
    {"name": "Aon", "slug": "aon", "emoji": "📊", "difficulty": "Medium",
     "description": "Professional services firm. Software roles in Assessment Solutions use C#/.NET, SQL Server and React.",
     "pattern": ["Online assessments testing coding and problem-solving", "Technical interview: DSA, OOP, SQL and the role's stack (C#/.NET, React)", "In-person HR / managerial interview for some roles", "Based on a public job posting." + NOTE],
     "papers": [paper("Aon Software Engineer Mock 1", "Medium", 30, {"Logical Reasoning": 4, "Computer Science": 6, "SQL": 5, "Programming": 5, "Quantitative Aptitude": 3}, 0),
                paper("Aon Software Engineer Mock 2", "Hard", 30, {"Logical Reasoning": 5, "Computer Science": 6, "SQL": 6, "Programming": 5, "Quantitative Aptitude": 2}, 2)]},
    {"name": "Amazon", "slug": "amazon", "emoji": "📦", "difficulty": "Hard",
     "description": "Heavy on data structures, algorithms and problem solving. Use the DSA practice section alongside these papers.",
     "pattern": ["Online assessment: coding problems on data structures and algorithms", "Work-style / behavioural simulation", "Technical interviews on DSA and problem solving", "Behavioural interview based on Leadership Principles", "Commonly reported pattern." + NOTE],
     "papers": [paper("Amazon OA Warm-up Mock 1", "Hard", 25, {"Logical Reasoning": 6, "Programming": 6, "Computer Science": 6}, 0, 60),
                paper("Amazon OA Warm-up Mock 2", "Hard", 25, {"Logical Reasoning": 5, "Programming": 7, "Computer Science": 6}, 1, 60)]},
]
for c in companies:
    c["description"] = c["description"].strip()
json.dump(companies, open(os.path.join(OUT, "companies.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)

# ------------------------------------------------------------------ DSA problems
problems = []
def add(title, slug, diff, topics, companies_, desc, constraints, examples, fn, starter, cases, ref):
    out = []
    for inp in cases:
        expected = ref(*[json.loads(json.dumps(a)) for a in inp])
        out.append({"input": inp, "expected": expected})
    problems.append({"title": title, "slug": slug, "difficulty": diff, "topics": topics, "companies": companies_,
                     "description": desc, "constraints": constraints, "examples": examples,
                     "functionName": fn, "starterCode": starter, "testCases": out})

def two_sum(nums, target):
    res = [[i, j] for i in range(len(nums)) for j in range(i + 1, len(nums)) if nums[i] + nums[j] == target]
    assert len(res) == 1, (nums, target, res)
    return res[0]
add("Two Sum", "two-sum", "Easy", "Arrays,Hash Table", "Amazon,TCS,Infosys,Accenture",
    "Given an array of integers `nums` and an integer `target`, return the indices of the two numbers that add up to `target`.\n\nEach input has exactly one solution and you may not use the same element twice. Return the two indices in ascending order.",
    "2 <= nums.length <= 10^4\n-10^9 <= nums[i], target <= 10^9\nExactly one valid answer exists.",
    "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]\nExplanation: nums[0] + nums[1] = 9.",
    "twoSum", "function twoSum(nums, target) {\n  // Return the two indices in ascending order\n\n}\n",
    [[[2, 7, 11, 15], 9], [[3, 2, 4], 6], [[3, 3], 6], [[1, 5, 3, 8, 12], 20], [[-1, -2, -3, -4, -5], -8], [[0, 4, 3, 0], 0]], two_sum)

def valid_paren(s):
    st, m = [], {')': '(', ']': '[', '}': '{'}
    for ch in s:
        if ch in '([{': st.append(ch)
        else:
            if not st or st.pop() != m[ch]: return False
    return not st
add("Valid Parentheses", "valid-parentheses", "Easy", "Stack,Strings", "Amazon,Wipro,Cognizant,Capgemini",
    "Given a string `s` containing only the characters ()[]{} determine whether the input string is valid.\n\nA string is valid if every open bracket is closed by the same type of bracket, and in the correct order.",
    "1 <= s.length <= 10^4\ns consists only of ()[]{}",
    "Input: s = \"()[]{}\"\nOutput: true\n\nInput: s = \"(]\"\nOutput: false",
    "isValid", "function isValid(s) {\n\n}\n",
    [["()"], ["()[]{}"], ["(]"], ["([)]"], ["{[]}"], ["((("], ["]"]], valid_paren)

def stock(prices):
    best, lo = 0, prices[0]
    for p in prices: lo = min(lo, p); best = max(best, p - lo)
    return best
add("Best Time to Buy and Sell Stock", "best-time-to-buy-and-sell-stock", "Easy", "Arrays,Greedy", "Amazon,Deloitte,Infosys",
    "You are given an array `prices` where prices[i] is the price of a stock on day i.\n\nChoose one day to buy and a later day to sell to maximise profit. Return the maximum profit, or 0 if no profit is possible.",
    "1 <= prices.length <= 10^5\n0 <= prices[i] <= 10^4",
    "Input: prices = [7,1,5,3,6,4]\nOutput: 5\nExplanation: Buy at 1, sell at 6.",
    "maxProfit", "function maxProfit(prices) {\n\n}\n",
    [[[7, 1, 5, 3, 6, 4]], [[7, 6, 4, 3, 1]], [[1, 2]], [[2, 4, 1]], [[3, 3, 3]], [[1, 2, 3, 4, 5]]], stock)

add("Contains Duplicate", "contains-duplicate", "Easy", "Arrays,Hash Table", "Aon,TCS,Wipro",
    "Given an integer array `nums`, return true if any value appears at least twice, and false if every element is distinct.",
    "1 <= nums.length <= 10^5\n-10^9 <= nums[i] <= 10^9",
    "Input: nums = [1,2,3,1]\nOutput: true",
    "containsDuplicate", "function containsDuplicate(nums) {\n\n}\n",
    [[[1, 2, 3, 1]], [[1, 2, 3, 4]], [[1, 1, 1, 3, 3, 4, 3, 2, 4, 2]], [[5]], [[-1, -1]]], lambda nums: len(set(nums)) != len(nums))

add("Valid Anagram", "valid-anagram", "Easy", "Strings,Hash Table,Sorting", "Cognizant,Capgemini,Accenture",
    "Given two strings `s` and `t`, return true if `t` is an anagram of `s`, and false otherwise.\n\nAn anagram uses all the original letters exactly once.",
    "1 <= s.length, t.length <= 5 * 10^4\ns and t consist of lowercase English letters",
    "Input: s = \"anagram\", t = \"nagaram\"\nOutput: true",
    "isAnagram", "function isAnagram(s, t) {\n\n}\n",
    [["anagram", "nagaram"], ["rat", "car"], ["a", "a"], ["ab", "a"], ["listen", "silent"]], lambda s, t: sorted(s) == sorted(t))

def bsearch(nums, target):
    lo, hi = 0, len(nums) - 1
    while lo <= hi:
        mid = (lo + hi) // 2
        if nums[mid] == target: return mid
        if nums[mid] < target: lo = mid + 1
        else: hi = mid - 1
    return -1
add("Binary Search", "binary-search", "Easy", "Binary Search,Arrays", "Infosys,TCS,Aon",
    "Given a sorted array of distinct integers `nums` and an integer `target`, return the index of `target` or -1 if it does not exist.\n\nYour algorithm must run in O(log n) time.",
    "1 <= nums.length <= 10^4\nAll values are distinct and sorted ascending.",
    "Input: nums = [-1,0,3,5,9,12], target = 9\nOutput: 4",
    "search", "function search(nums, target) {\n\n}\n",
    [[[-1, 0, 3, 5, 9, 12], 9], [[-1, 0, 3, 5, 9, 12], 2], [[5], 5], [[1, 3, 5, 7], 7], [[1, 3, 5, 7], 1], [[2, 4], 3]], bsearch)

def climb(n):
    a, b = 1, 1
    for _ in range(n - 1): a, b = b, a + b
    return b
add("Climbing Stairs", "climbing-stairs", "Easy", "Dynamic Programming,Math", "Amazon,Wipro,Capgemini",
    "You are climbing a staircase with `n` steps. Each time you can climb 1 or 2 steps. In how many distinct ways can you reach the top?",
    "1 <= n <= 45",
    "Input: n = 3\nOutput: 3\nExplanation: 1+1+1, 1+2, 2+1.",
    "climbStairs", "function climbStairs(n) {\n\n}\n",
    [[1], [2], [3], [5], [10], [30]], climb)

def move_zeroes(nums):
    return [x for x in nums if x != 0] + [0] * nums.count(0)
add("Move Zeroes", "move-zeroes", "Easy", "Arrays,Two Pointers", "TCS,Cognizant,Aon",
    "Given an integer array `nums`, move all 0s to the end while keeping the relative order of the non-zero elements.\n\nReturn the resulting array.",
    "1 <= nums.length <= 10^4",
    "Input: nums = [0,1,0,3,12]\nOutput: [1,3,12,0,0]",
    "moveZeroes", "function moveZeroes(nums) {\n  // Return the array with zeroes moved to the end\n\n}\n",
    [[[0, 1, 0, 3, 12]], [[0]], [[1, 2, 3]], [[0, 0, 1]], [[4, 0, 5, 0, 6]]], move_zeroes)

def lcp(strs):
    p = strs[0]
    for s in strs[1:]:
        while not s.startswith(p): p = p[:-1]
    return p
add("Longest Common Prefix", "longest-common-prefix", "Easy", "Strings", "Wipro,Infosys,Deloitte",
    "Write a function to find the longest common prefix string among an array of strings. If there is none, return an empty string.",
    "1 <= strs.length <= 200\n0 <= strs[i].length <= 200",
    "Input: strs = [\"flower\",\"flow\",\"flight\"]\nOutput: \"fl\"",
    "longestCommonPrefix", "function longestCommonPrefix(strs) {\n\n}\n",
    [[["flower", "flow", "flight"]], [["dog", "racecar", "car"]], [["a"]], [["interview", "internet", "interval"]], [["same", "same"]]], lcp)

def kadane(nums):
    best = cur = nums[0]
    for x in nums[1:]: cur = max(x, cur + x); best = max(best, cur)
    return best
add("Maximum Subarray", "maximum-subarray", "Medium", "Arrays,Dynamic Programming", "Amazon,Accenture,Aon",
    "Given an integer array `nums`, find the contiguous subarray (containing at least one number) with the largest sum and return that sum.",
    "1 <= nums.length <= 10^5\n-10^4 <= nums[i] <= 10^4",
    "Input: nums = [-2,1,-3,4,-1,2,1,-5,4]\nOutput: 6\nExplanation: [4,-1,2,1] has the largest sum.",
    "maxSubArray", "function maxSubArray(nums) {\n\n}\n",
    [[[-2, 1, -3, 4, -1, 2, 1, -5, 4]], [[1]], [[5, 4, -1, 7, 8]], [[-3, -2, -1]], [[-2, -1]]], kadane)

def merge(intervals):
    res = []
    for s, e in sorted(intervals):
        if res and s <= res[-1][1]: res[-1][1] = max(res[-1][1], e)
        else: res.append([s, e])
    return res
add("Merge Intervals", "merge-intervals", "Medium", "Arrays,Sorting,Intervals", "Amazon,Deloitte",
    "Given an array of `intervals` where intervals[i] = [start, end], merge all overlapping intervals and return the non-overlapping intervals sorted by start.",
    "1 <= intervals.length <= 10^4\n0 <= start <= end <= 10^4",
    "Input: intervals = [[1,3],[2,6],[8,10],[15,18]]\nOutput: [[1,6],[8,10],[15,18]]",
    "merge", "function merge(intervals) {\n\n}\n",
    [[[[1, 3], [2, 6], [8, 10], [15, 18]]], [[[1, 4], [4, 5]]], [[[1, 4], [0, 4]]], [[[1, 4], [2, 3]]], [[[5, 6], [1, 2]]]], merge)

def lswr(s):
    last, start, best = {}, 0, 0
    for i, ch in enumerate(s):
        if ch in last and last[ch] >= start: start = last[ch] + 1
        last[ch] = i; best = max(best, i - start + 1)
    return best
add("Longest Substring Without Repeating Characters", "longest-substring-without-repeating-characters", "Medium", "Strings,Sliding Window,Hash Table", "Amazon,Infosys,Aon",
    "Given a string `s`, find the length of the longest substring without repeating characters.",
    "0 <= s.length <= 5 * 10^4",
    "Input: s = \"abcabcbb\"\nOutput: 3\nExplanation: \"abc\" has length 3.",
    "lengthOfLongestSubstring", "function lengthOfLongestSubstring(s) {\n\n}\n",
    [["abcabcbb"], ["bbbbb"], ["pwwkew"], [""], ["dvdf"], ["abba"]], lswr)

def pes(nums):
    n = len(nums); res = [1] * n
    for i in range(1, n): res[i] = res[i - 1] * nums[i - 1]
    r = 1
    for i in range(n - 1, -1, -1): res[i] *= r; r *= nums[i]
    return res
add("Product of Array Except Self", "product-of-array-except-self", "Medium", "Arrays,Prefix Sum", "Amazon,Accenture",
    "Given an integer array `nums`, return an array `answer` where answer[i] is the product of all elements of `nums` except nums[i].\n\nSolve it in O(n) time without using division.",
    "2 <= nums.length <= 10^5\n-30 <= nums[i] <= 30",
    "Input: nums = [1,2,3,4]\nOutput: [24,12,8,6]",
    "productExceptSelf", "function productExceptSelf(nums) {\n\n}\n",
    [[[1, 2, 3, 4]], [[-1, 1, 0, -3, 3]], [[2, 3]], [[1, 1, 1]], [[0, 0]]], pes)

def coin(coins, amount):
    INF = 10 ** 9; dp = [0] + [INF] * amount
    for a in range(1, amount + 1):
        for c in coins:
            if c <= a: dp[a] = min(dp[a], dp[a - c] + 1)
    return -1 if dp[amount] >= INF else dp[amount]
add("Coin Change", "coin-change", "Medium", "Dynamic Programming", "Amazon,Cognizant",
    "You are given an array `coins` of coin denominations and an integer `amount`.\n\nReturn the fewest number of coins needed to make up that amount, or -1 if it cannot be made. You have an unlimited supply of each coin.",
    "1 <= coins.length <= 12\n0 <= amount <= 10^4",
    "Input: coins = [1,2,5], amount = 11\nOutput: 3\nExplanation: 5 + 5 + 1.",
    "coinChange", "function coinChange(coins, amount) {\n\n}\n",
    [[[1, 2, 5], 11], [[2], 3], [[1], 0], [[1, 3, 4], 6], [[186, 419, 83, 408], 6249]], coin)

def rain(h):
    l, r, lm, rm, tot = 0, len(h) - 1, 0, 0, 0
    while l < r:
        if h[l] < h[r]:
            lm = max(lm, h[l]); tot += lm - h[l]; l += 1
        else:
            rm = max(rm, h[r]); tot += rm - h[r]; r -= 1
    return tot
add("Trapping Rain Water", "trapping-rain-water", "Hard", "Arrays,Two Pointers,Dynamic Programming", "Amazon,Deloitte",
    "Given `n` non-negative integers representing an elevation map where each bar has width 1, compute how much water it can trap after raining.",
    "1 <= n <= 2 * 10^4\n0 <= height[i] <= 10^5",
    "Input: height = [0,1,0,2,1,0,1,3,2,1,2,1]\nOutput: 6",
    "trap", "function trap(height) {\n\n}\n",
    [[[0, 1, 0, 2, 1, 0, 1, 3, 2, 1, 2, 1]], [[4, 2, 0, 3, 2, 5]], [[1, 2, 3]], [[3, 0, 3]], [[2, 0, 2]]], rain)

assert len({p["slug"] for p in problems}) == len(problems)
json.dump(problems, open(os.path.join(OUT, "problems.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print("questions:", sum(len(v) for v in pool.values()), "| companies:", len(companies), "| problems:", len(problems))
