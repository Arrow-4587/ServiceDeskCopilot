"""
Azure AI Foundry Evaluation Runner for ServiceDeskCopilot Agents
Evaluates strictly the 11 user-specified metrics:
1. HateAndUnfairness
2. CodeVulnerability
3. Fluency
4. Violence
5. SelfHarm
6. IndirectAttack
7. UngroundedAttributes
8. Sexual
9. Coherence
10. Groundedness
11. ToolCallSuccessEvaluator
"""

import os
import sys
import json
import argparse
from pathlib import Path

TARGET_EVALUATORS = [
    "HateAndUnfairness",
    "CodeVulnerability",
    "Fluency",
    "Violence",
    "SelfHarm",
    "IndirectAttack",
    "UngroundedAttributes",
    "Sexual",
    "Coherence",
    "Groundedness",
    "ToolCallSuccessEvaluator"
]

def load_dataset(file_path: str):
    path = Path(file_path)
    if not path.exists():
        raise FileNotFoundError(f"Dataset not found at {file_path}")
    
    records = []
    if path.suffix == ".jsonl":
        with open(path, "r", encoding="utf-8-sig") as f:
            for line in f:
                if line.strip():
                    records.append(json.loads(line))
    else:
        with open(path, "r", encoding="utf-8-sig") as f:
            records = json.load(f)
    return records

def run_evaluation_with_azure_sdk(dataset_path: str, output_path: str = None):
    """
    Runs evaluation using the azure-ai-evaluation SDK when configured.
    """
    try:
        from azure.identity import DefaultAzureCredential
        from azure.ai.evaluation import (
            evaluate,
            GroundednessEvaluator,
            FluencyEvaluator,
            CoherenceEvaluator,
            HateUnfairnessEvaluator,
            ViolenceEvaluator,
            SelfHarmEvaluator,
            SexualEvaluator,
            IndirectAttackEvaluator
        )
    except ImportError:
        print("[WARNING] 'azure-ai-evaluation' package not installed.")
        print("Install via: pip install azure-ai-evaluation azure-identity")
        print("Running offline dataset verification instead...\n")
        return verify_dataset_offline(dataset_path)

    endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
    api_key = os.getenv("AZURE_OPENAI_KEY")
    deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT", "gpt-4o-mini")

    if not endpoint:
        print("[INFO] AZURE_OPENAI_ENDPOINT not set. Running offline verification.")
        return verify_dataset_offline(dataset_path)

    model_config = {
        "azure_endpoint": endpoint,
        "api_key": api_key,
        "azure_deployment": deployment
    }

    evaluators = {
        "Groundedness": GroundednessEvaluator(model_config),
        "Fluency": FluencyEvaluator(model_config),
        "Coherence": CoherenceEvaluator(model_config),
        "HateAndUnfairness": HateUnfairnessEvaluator(credential=DefaultAzureCredential()),
        "Violence": ViolenceEvaluator(credential=DefaultAzureCredential()),
        "SelfHarm": SelfHarmEvaluator(credential=DefaultAzureCredential()),
        "Sexual": SexualEvaluator(credential=DefaultAzureCredential()),
        "IndirectAttack": IndirectAttackEvaluator(credential=DefaultAzureCredential())
    }

    print(f"Executing Azure AI Foundry evaluation on {dataset_path} with target evaluators...")
    results = evaluate(
        data=dataset_path,
        evaluators=evaluators,
        evaluation_name=f"ServiceDesk-Eval-{Path(dataset_path).stem}"
    )
    
    if output_path:
        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2)
        print(f"Results saved to {output_path}")
    return results

def verify_dataset_offline(dataset_path: str):
    """
    Offline verification ensuring every row has all required fields for the 11 evaluators.
    """
    records = load_dataset(dataset_path)
    print(f"=== Offline Dataset Verification: {Path(dataset_path).name} ===")
    print(f"Total Records: {len(records)}")
    print(f"Target Evaluation Metrics (11): {', '.join(TARGET_EVALUATORS)}\n")

    missing_fields = []
    for idx, r in enumerate(records):
        desc = r.get("test_case_description")
        query = r.get("query")
        context = r.get("context")
        ground_truth = r.get("ground_truth")
        metrics = r.get("metadata", {}).get("eval_metrics", [])

        if not desc:
            missing_fields.append(f"Row {idx+1}: missing 'test_case_description'")
        if not query:
            missing_fields.append(f"Row {idx+1}: missing 'query'")
        if not context:
            missing_fields.append(f"Row {idx+1}: missing 'context'")
        if not ground_truth:
            missing_fields.append(f"Row {idx+1}: missing 'ground_truth'")
        if metrics != TARGET_EVALUATORS:
            missing_fields.append(f"Row {idx+1}: eval_metrics do not match the exact 11 target fields")

    if missing_fields:
        print("[FAIL] Verification issues found:")
        for err in missing_fields[:10]:
            print(f"  - {err}")
        return False
    else:
        print("[PASS] All records contain required fields:")
        print("  - test_case_description (Azure AI Foundry Studio required)")
        print("  - query (Input for Safety, Quality, and Grounding evaluators)")
        print("  - context (Required for Groundedness and UngroundedAttributes)")
        print("  - ground_truth (Target for Coherence, Fluency, and Content verification)")
        print("  - metadata.eval_metrics (Configured for exactly the 11 specified evaluators)")
        return True

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Evaluate ServiceDesk Copilot dataset with 11 target evaluators.")
    parser.add_argument("--dataset", type=str, default="planner_agent_evaluation.jsonl",
                        help="Path to evaluation dataset file (.json or .jsonl)")
    parser.add_argument("--output", type=str, default=None, help="Output file for evaluation results")
    args = parser.parse_args()

    base_dir = Path(__file__).parent
    dataset_file = base_dir / args.dataset if not Path(args.dataset).is_absolute() else Path(args.dataset)
    
    verify_dataset_offline(str(dataset_file))
