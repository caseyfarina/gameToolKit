#!/usr/bin/env python3
"""
Parse Unity scene files to extract UnityEvent wiring documentation
"""
import re
import sys

def parse_scene(filename):
    """Parse Unity scene file and extract event connections"""
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # First pass: Build GameObject ID -> Name mapping
    gameobjects = {}
    current_id = None
    for i, line in enumerate(lines):
        # Match GameObject header
        if match := re.match(r'--- !u!1 &(\d+)', line):
            current_id = match.group(1)
        # Match GameObject name
        elif current_id and 'm_Name:' in line:
            name = line.split('m_Name:')[1].strip()
            gameobjects[current_id] = name
            current_id = None

    # Second pass: Find MonoBehaviour components and their events
    components = []
    i = 0
    while i < len(lines):
        line = lines[i]

        # Find MonoBehaviour component
        if match := re.match(r'--- !u!114 &(\d+)', line):
            comp_id = match.group(1)
            comp_data = {'id': comp_id, 'events': []}

            # Find m_GameObject reference
            j = i
            while j < min(i + 20, len(lines)):
                if 'm_GameObject: {fileID:' in lines[j]:
                    if go_match := re.search(r'fileID: (\d+)', lines[j]):
                        go_id = go_match.group(1)
                        comp_data['gameobject'] = gameobjects.get(go_id, f'Unknown-{go_id}')
                    break
                j += 1

            # Find component type
            j = i
            while j < min(i + 30, len(lines)):
                if 'm_Script:' in lines[j]:
                    # Look ahead for guid
                    k = j
                    while k < min(j + 3, len(lines)):
                        if 'guid:' in lines[k]:
                            # Component type usually follows in m_Name field or we extract from class
                            pass
                        k += 1
                    break
                j += 1

            # Find MonoBehaviour class name (appears after m_Name: near m_Script)
            j = i
            comp_type = None
            while j < min(i + 50, len(lines)):
                # Look for the MonoBehaviour class declaration pattern
                if re.match(r'^  [a-zA-Z]', lines[j]) and ':' not in lines[j] and not lines[j].strip().startswith('m_'):
                    potential_type = lines[j].strip()
                    if potential_type and not potential_type.startswith('-'):
                        comp_type = potential_type
                        break
                j += 1

            if comp_type:
                comp_data['type'] = comp_type

            # Find all events in this component
            j = i
            while j < len(lines):
                line_j = lines[j]

                # Stop at next component
                if j > i and line_j.startswith('---'):
                    break

                # Look for event declarations (UnityEvent pattern)
                if re.match(r'  \w+:$', line_j):
                    event_name = line_j.strip().rstrip(':')

                    # Check if next lines contain m_PersistentCalls with actual calls
                    if j + 2 < len(lines) and 'm_PersistentCalls:' in lines[j + 1]:
                        if 'm_Calls:' in lines[j + 2]:
                            # Parse the calls
                            k = j + 3
                            while k < len(lines) and lines[k].startswith('      -'):
                                # Found a call
                                call_data = {'event': event_name}

                                # Extract target
                                if 'm_Target: {fileID:' in lines[k]:
                                    if target_match := re.search(r'fileID: (\d+)', lines[k]):
                                        target_id = target_match.group(1)
                                        call_data['target_name'] = gameobjects.get(target_id, f'Unknown-{target_id}')

                                # Look ahead for method details
                                m = k
                                while m < min(k + 10, len(lines)) and not lines[m].startswith('      -'):
                                    if 'm_TargetAssemblyTypeName:' in lines[m]:
                                        type_str = lines[m].split('m_TargetAssemblyTypeName:')[1].strip()
                                        call_data['target_type'] = type_str.split(',')[0]
                                    elif 'm_MethodName:' in lines[m]:
                                        method = lines[m].split('m_MethodName:')[1].strip()
                                        call_data['method'] = method
                                    elif 'm_Arguments:' in lines[m]:
                                        # Check for arguments
                                        arg_line = m
                                        while arg_line < min(m + 10, len(lines)):
                                            if 'm_IntArgument:' in lines[arg_line]:
                                                arg_val = lines[arg_line].split(':')[1].strip()
                                                call_data['arg'] = f'int({arg_val})'
                                            elif 'm_FloatArgument:' in lines[arg_line] and lines[arg_line].split(':')[1].strip() != '0':
                                                arg_val = lines[arg_line].split(':')[1].strip()
                                                call_data['arg'] = f'float({arg_val})'
                                            elif 'm_StringArgument:' in lines[arg_line]:
                                                arg_val = lines[arg_line].split(':')[1].strip()
                                                if arg_val:
                                                    call_data['arg'] = f'"{arg_val}"'
                                            elif 'm_BoolArgument:' in lines[arg_line]:
                                                arg_val = lines[arg_line].split(':')[1].strip()
                                                call_data['arg'] = arg_val
                                            arg_line += 1
                                    m += 1

                                if 'method' in call_data:
                                    comp_data['events'].append(call_data)

                                k = m

                j += 1

            if comp_data['events']:
                components.append(comp_data)

        i += 1

    return components

def print_documentation(scene_file):
    """Print formatted documentation for a scene"""
    print(f"\n{'='*70}")
    print(f"SCENE: {scene_file}")
    print(f"{'='*70}\n")

    components = parse_scene(scene_file)

    if not components:
        print("No UnityEvent wirings found.\n")
        return

    print(f"Found {len(components)} component(s) with event wiring:\n")

    for i, comp in enumerate(components, 1):
        go_name = comp.get('gameobject', 'Unknown')
        comp_type = comp.get('type', 'MonoBehaviour')

        print(f"{i}. GameObject: {go_name}")
        if 'type' in comp:
            print(f"   Component: {comp_type}")
        print()

        for event in comp['events']:
            arg_str = f"({event['arg']})" if 'arg' in event else "()"
            print(f"   {event['event']}")
            print(f"   → {event.get('target_name', '?')}.{event.get('target_type', '?')}.{event.get('method', '?')}{arg_str}")
            print()

        print()

# Process all three scenes
for scene in ['collectionExample.unity', 'projectileExample.unity', 'puzzleExample.unity']:
    try:
        print_documentation(scene)
    except Exception as e:
        print(f"Error processing {scene}: {e}")
        import traceback
        traceback.print_exc()
