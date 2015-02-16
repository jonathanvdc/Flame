from DoubleNode import *

class DoubleNode:
    """ A node that points to two other nodes.
        It is used as a node in a doubly linked list. """

    def __init__(self, Value):
        """ Creates a new doubly linked node containing the given value. """
        self.successor_value = None
        self.predecessor_value = None
        self.value = Value

    def set_predecessor(self, Node):
        """ Sets this node's predecessor to the given node, and sets the given node's successor to this node, if 'Node' is not 'None'. """
        # Post:
        # If 'Node' equals 'None', sets this node's predecessor to 'None'.
        # Otherwise, this node's predecessor to the given node, and sets the given node's successor to this node.
        self.predecessor = Node
        if Node is not None:
            Node.successor = self

    def set_successor(self, Node):
        """ Sets this node's successor to the given node, and sets the given node's predecessor to this node, if 'Node' is not 'None'. """
        # Post:
        # If 'Node' equals 'None', sets this node's successor to 'None'.
        # Otherwise, this node's successor to the given node, and sets the given node's predecessor to this node.
        self.successor = Node
        if Node is not None:
            Node.predecessor = self

    def insert_after(self, Value):
        """ Inserts a node containing the provided value after this node. """
        # Post:
        # Creates a new node containing the provided value, sets its successor to this node's successor and its predecessor to this node, sets this node's successor to the newly created node, and sets this node's
        # Remarks:
        # This operation corresponds to (part of) a list insert for linked lists.
        nextVal = DoubleNode(Value)
        nextVal.set_successor(self.successor)
        nextVal.set_predecessor(self)

    def insert_before(self, Value):
        """ Inserts an item containing the provided node right before this node. """
        # Post:
        # Creates a new node containing the provided value, sets its successor to this node and its predecessor to this node's predecessor, sets this node's predecessor's successor to the newly created node, and set this node's predecessor to the new node.
        prevVal = DoubleNode(Value)
        prevVal.set_predecessor(self.predecessor)
        prevVal.set_successor(self)

    def remove(self):
        """ Removes this node from the linked chain. """
        # Pre:
        # For this method to change the chain's state, this node should have at least one successor or a predecessor;
        # Post:
        # If this node's predecessor is not 'None', sets this node's predecessor's successor to this node's successor, and vice-versa.
        # Otherwise, if this node's successor is not 'None', sets this node's successor's predecessor to 'None'.
        if self.predecessor is not None:
            self.predecessor.set_successor(self.successor)
        elif self.successor is not None:
            self.successor.set_predecessor(None)

    @property
    def predecessor(self):
        """ Gets the node's predecessor, if any. """
        return self.predecessor_value

    @predecessor.setter
    def predecessor(self, value):
        """ Sets the node's predecessor, if any. """
        self.predecessor_value = value

    @property
    def successor(self):
        """ Gets the node's successor, if any. """
        return self.successor_value

    @successor.setter
    def successor(self, value):
        """ Sets the node's successor, if any. """
        self.successor_value = value

    @property
    def value(self):
        """ Gets the node's value. """
        return self.value_value

    @value.setter
    def value(self, value):
        """ Sets the node's value. """
        self.value_value = value

    @property
    def tail(self):
        """ Gets the "tail" node of this linked chain. """
        # Post:
        # The tail of a list node is defined recursively as follows.
        # If the current node has no successor, return the current node.
        # Otherwise, return the current node's successor's tail.
        if self.successor is None:
            return self
        else:
            return self.successor.tail