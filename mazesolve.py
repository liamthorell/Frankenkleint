import json
import networkx as nx
from tqdm import tqdm
from itertools import product

f = open("maze.json", "r")
size = 30
maze = json.load(f)
nodes = []


class Node:
    def __init__(self, x, y, z, xi, node_type):
        self.x = x
        self.y = y
        self.z = z
        self.xi = xi
        self.type = node_type

    def __eq__(self, other):
        return (
            self.x == other.x
            and self.y == other.y
            and self.z == other.z
            and self.xi == other.xi
            and self.type == other.type
        )

    def __hash__(self):
        return hash((self.x, self.y, self.z, self.xi, self.type))


start = Node(0, 0, 0, 0, "air")
end = Node(14, 14, 16, 14, "air")
nodes.append(start)
nodes.append(end)

for chunk in maze:
    for z in range(15):
        for x in range(15):
            block = chunk["map"][z][x]

            if block["type"] != "air":
                continue

            nodes.append(
                Node(
                    (chunk["x"] * 15) + x,
                    chunk["y"],
                    (chunk["z"] * 15) + z,
                    chunk["xi"],
                    block["type"],
                )
            )

nodes_set = set(nodes)
edges = []

for node_i in tqdm(range(len(nodes))):
    node = nodes[node_i]
    if node.type == "air":
        for i, j, k, l in product(range(-1, 2), repeat=4):
            if i == j == k == l == 0:
                continue

            if any(
                coord < 0 or coord >= size
                for coord in (node.x + i, node.y + j, node.z + k, node.xi + l)
            ):
                continue

            new_node = Node(node.x + i, node.y + j, node.z + k, node.xi + l, "air")

            if new_node in nodes_set:
                edges.append((node, new_node))

G = nx.Graph()
G.add_edges_from(edges)
path = nx.shortest_path(G, start, end)

total = ""
before = Node(0, 0, 0, 0, "air")

with open("path.json", "w") as f:
    p_all = []
    for node in path:
        p = {
            "x": node.x - before.x,
            "y": node.y - before.y,
            "z": node.z - before.z,
            "xi": node.xi - before.xi,
        }
        p_all.append(p)
        before = node

        total += f"{node.x} {node.y} {node.z} {node.xi}\n"

    json.dump(p_all, f)

open("path.txt", "w").write(total)
print("Path Length", len(path))
