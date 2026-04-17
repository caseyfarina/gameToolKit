# Desk Check

Perform a desk check (manual trace review) of the specified file or component. Trace through every public method with forecasted inputs to verify logic without executing it.

## Target

$ARGUMENTS

If no argument is provided, ask which script or component to review.

## Process

1. **Read the target file** completely. If it has a custom editor, read that too.

2. **Identify all public entry points**: public methods, Unity lifecycle methods (Start, Awake, OnEnable, Update, OnDestroy, OnTriggerEnter, etc.), and coroutines.

3. **For each entry point, build a trace table** forecasting specific input values through the logic:
   - **Happy path**: Default/expected values
   - **Boundary values**: 0, -1, max, min, null, empty
   - **State transitions**: What happens when called in sequence (e.g., damage then heal, enable then disable)
   - **Cross-method interactions**: Does method A leave state that breaks method B?
   - **Guard clauses**: Do early returns cover all invalid inputs?
   - **Flag ordering**: Are boolean flags set before or after code that reads them? (Common source of silent bugs)
   - **Event firing**: Do UnityEvents fire at the right time with correct parameters?
   - **Null safety**: Are runtime references (UI elements, components) null-checked before use?

4. **For editor scripts**, verify:
   - Every `FindProperty("fieldName")` string matches an actual `[SerializeField]` field name
   - Conditional UI sections show/hide the correct fields
   - Preview lifecycle (create/update/destroy) handles all toggle combinations

5. **Classify each finding**:
   - **BUG**: Logic error that produces wrong behavior. State whether pre-existing or newly introduced.
   - **DISPLAY BUG**: UI/visual doesn't update when it should.
   - **EDGE CASE**: Technically correct but may surprise users. Note whether it matters for this project's audience (students).
   - **OK**: Traced and verified correct.

6. **Present results** as:
   - Per-method trace tables showing input state, expected result, actual result, and verdict
   - A summary section listing all bugs and edge cases with root cause and severity
   - If bugs are found, describe the fix but **ask before applying it**

## Output Format

Use tables for trace results. Lead with the most important findings. Keep verdicts to one word (OK, BUG, EDGE CASE) with details in a summary section below the tables.
