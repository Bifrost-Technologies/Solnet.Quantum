/// Quantum Teleportation
///
/// # Description
/// Quantum teleportation provides a way of moving a quantum state from one
/// location to another without having to move physical particle(s) along with
/// it. This is done with the help of previously shared quantum entanglement
/// between the sending and the receiving locations, and classical
/// communication.
///
/// This Q# program implements quantum teleportation.
namespace QSOL {

    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Measurement;
  

    /// # Summary
    /// Sends the state of one qubit to a target qubit by using teleportation.
    ///
    /// Notice that after calling Teleport, the state of `message` is collapsed.
    ///
    /// # Input
    /// ## message
    /// A qubit whose state we wish to send.
    /// ## target
    /// A qubit initially in the |0〉 state that we want to send
    /// the state of message to.
    operation Teleport(message : Qubit, target : Qubit) : Unit {
        // Allocate an auxiliary qubit.
        use auxiliary = Qubit();

        // Create some entanglement that we can use to send our message.
        H(auxiliary);
        CNOT(auxiliary, target);

        // Encode the message into the entangled pair.
        CNOT(message, auxiliary);
        H(message);

        // Measure the qubits to extract the classical data we need to decode
        // the message by applying the corrections on the target qubit
        // accordingly.
        if M(auxiliary) == One {
            X(target);
        }

        if M(message) == One {
            Z(target);
        }

        // Reset auxiliary qubit before releasing.
        Reset(auxiliary);
    }

    /// # Summary
    /// Sets a qubit in state |0⟩ to |+⟩.
    operation SetToPlus(q : Qubit) : Unit is Adj + Ctl {
        H(q);
    }

    /// # Summary
    /// Sets a qubit in state |0⟩ to |−⟩.
    operation SetToMinus(q : Qubit) : Unit is Adj + Ctl {
        X(q);
        H(q);
    }
}