# Fixtures

expected.json defines trace-command expectations, not validate-command expectations. In particular, missing-target is structurally valid for `validate`, and inconsistent-order is a valid acyclic graph for `validate`. Invalid cases can yield more than one diagnostic; assert that diagnosticCode occurs, not necessarily that it is the only diagnostic.

single-root, chain, diamond, reversed diamond, forward references, independent roots, shared ancestors, deeper diamond, candidate rejection, redundant base order, C3 conflict, ancestor failure, disconnected inconsistency and case-sensitive names cover the core semantic cases. The remaining fixtures exercise structural failures.

`shared-ancestors` demonstrates rejection of A until D is chosen. `candidate-rejection` demonstrates rejecting Y until B is chosen. `redundant-base-invalid` shows that [O,A] conflicts when A already inherits O. Acyclicity alone does not imply a C3 order exists.

Malformed JSON and duplicate JSON property tests belong in handcrafted test strings; all fixture files intentionally remain syntactically valid JSON.
