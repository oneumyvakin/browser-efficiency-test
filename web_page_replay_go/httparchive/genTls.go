package main

import (
	"fmt"
	"io/ioutil"
	"encoding/pem"
	"crypto/x509"
	"math/big"
	"time"
	"crypto/rand"
	"os"
	"crypto/rsa"
	"crypto"
	"encoding/asn1"
	"crypto/x509/pkix"
	"bytes"
	"bufio"
	"net"
	"net/url"
	"encoding/json"
)

const (
	caRegistryFile = "caRegistry.json"
)

type caRegistry struct {
	LastSerialNumber CertificateSerialNumber
	Domains map[DomainName]CaRegistryEntry
}

type DomainName string

type CertificateSerialNumber int64

type CaRegistryEntry struct {
	DomainName DomainName
	SerialNumber CertificateSerialNumber
}

func loadCaRegistry() (caRegistry, error) {
	caReg := caRegistry{}

	caRegFile, err := os.Open(caRegistryFile)
	if os.IsNotExist(err) {
		caReg.Domains = map[DomainName]CaRegistryEntry{}
		return caReg, nil // Not exists is OK
	}
	if err != nil {
		return caReg, err
	}
	defer caRegFile.Close()

	err = json.NewDecoder(caRegFile).Decode(&caReg)
	if err != nil {
		return caReg, err
	}

	if caReg.Domains == nil {
		caReg.Domains = map[DomainName]CaRegistryEntry{}
	}
	return caReg, nil
}

func saveCaRegistry(ca caRegistry) (error) {
	caRegFile, err := os.OpenFile(caRegistryFile, os.O_CREATE|os.O_WRONLY|os.O_TRUNC, 0666)
	if err != nil {
		return fmt.Errorf("os.OpenFile: %s", err)
	}
	defer caRegFile.Close()
	err = json.NewEncoder(caRegFile).Encode(ca)
	if err != nil {
		fmt.Errorf("json.NewEncoder(caRegFile).Encode(ca): %s", err)
	}
	return nil
}

func crsBytesToCrtFile(caCrt string, caKey string, inCsr []byte, serialNumber int64, outCrt string) {
	fmt.Println(caCrt, caKey, "<bytes>", outCrt)

	// load CA key pair
	//      public key
	caPublicKeyFile, err := ioutil.ReadFile(caCrt)
	if err != nil {
		panic(err)
	}
	pemBlock, _ := pem.Decode(caPublicKeyFile)
	if pemBlock == nil {
		panic("pem.Decode failed")
	}
	caCRT, err := x509.ParseCertificate(pemBlock.Bytes)
	if err != nil {
		panic(err)
	}

	//      private key
	caPrivateKeyFile, err := ioutil.ReadFile(caKey)
	if err != nil {
		panic(err)
	}
	pemBlockKey, _ := pem.Decode(caPrivateKeyFile)
	if pemBlockKey == nil {
		panic("pem.Decode failed")
	}

	caPrivateKey, err := x509.ParsePKCS1PrivateKey(pemBlockKey.Bytes)
	if err != nil {
		panic(err)
	}

	pemBlock, _ = pem.Decode(inCsr)
	if pemBlock == nil {
		panic("pem.Decode failed")
	}
	clientCSR, err := x509.ParseCertificateRequest(pemBlock.Bytes)
	if err != nil {
		panic(err)
	}
	if err = clientCSR.CheckSignature(); err != nil {
		panic(err)
	}

	// create client certificate template
	clientCRTTemplate := x509.Certificate{
		Signature:          clientCSR.Signature,
		SignatureAlgorithm: clientCSR.SignatureAlgorithm,

		PublicKeyAlgorithm: clientCSR.PublicKeyAlgorithm,
		PublicKey:          clientCSR.PublicKey,

		SerialNumber: big.NewInt(int64(serialNumber)),
		Issuer:       caCRT.Subject,
		Subject:      clientCSR.Subject,
		DNSNames:     clientCSR.DNSNames,
		NotBefore:    time.Now(),
		NotAfter:     time.Now().Add(365 * 24 * time.Hour),
		KeyUsage:     x509.KeyUsageDigitalSignature,
		//ExtKeyUsage:  []x509.ExtKeyUsage{x509.ExtKeyUsageAny},
	}

	// create client certificate from template and CA public key
	clientCRTRaw, err := x509.CreateCertificate(rand.Reader, &clientCRTTemplate, caCRT, clientCSR.PublicKey, caPrivateKey)
	if err != nil {
		panic(err)
	}

	// save the certificate
	clientCRTFile, err := os.Create(outCrt)
	if err != nil {
		panic(err)
	}
	pem.Encode(clientCRTFile, &pem.Block{Type: "CERTIFICATE", Bytes: clientCRTRaw})
	clientCRTFile.Close()
}

func createAndSaveDomainPrivateKey(path string) (*rsa.PrivateKey, error) {
	reader := rand.Reader
	bitSize := 2048

	key, err := rsa.GenerateKey(reader, bitSize)
	if err != nil {
		return nil, err
	}

	err = savePrivateKey(path, key)
	if err != nil {
		return nil, err
	}

	return key, nil
}

func savePrivateKey(fileName string, key *rsa.PrivateKey) error {
	outFile, err := os.Create(fileName)
	if err != nil {
		return err
	}
	defer outFile.Close()

	var privateKey = &pem.Block{
		Type:  "RSA PRIVATE KEY",
		Bytes: x509.MarshalPKCS1PrivateKey(key),
	}

	err = pem.Encode(outFile, privateKey)
	if err != nil {
		return err
	}

	return nil
}

func createCSR(key crypto.PrivateKey, domainName string) (_ []byte, err error) {
	oidExtensionSubjectAltName := []int{2, 5, 29, 17}
	oidExtensionAuthorityInfoAccess := []int{1, 3, 6, 1, 5, 5, 7, 1, 1}
	oidExtensionRequest := asn1.ObjectIdentifier{1, 2, 840, 113549, 1, 9, 14}

	sanContents, err := marshalSANs([]string{domainName}, nil, nil, nil)
	if err != nil {
		return nil, err
	}

	subj := pkix.Name{
		CommonName:         domainName,
		Country:            []string{"-"},
		Province:           []string{"-"},
		Locality:           []string{"-"},
		Organization:       []string{"Nebulo"},
		OrganizationalUnit: []string{"Nebulo Clients CA"},
	}

	asn1Subj, err := asn1.Marshal(subj.ToRDNSequence())
	if err != nil {
		return nil, fmt.Errorf("unable to marshal asn1: %v", err)
	}

	template := x509.CertificateRequest{
		RawSubject:         asn1Subj,
		SignatureAlgorithm: x509.SHA512WithRSA,
	}
	template.Attributes = []pkix.AttributeTypeAndValueSET{
		{
			Type: oidExtensionRequest,
			Value: [][]pkix.AttributeTypeAndValue{
				{
					{
						Type:  oidExtensionAuthorityInfoAccess,
						Value: []byte("foo"),
					},
				},
			},
		},
	}

	template.Attributes[0].Value[0] = append(template.Attributes[0].Value[0], pkix.AttributeTypeAndValue{
		Type:  oidExtensionSubjectAltName,
		Value: sanContents,
	})

	csrBytes, err := x509.CreateCertificateRequest(rand.Reader, &template, key)
	if err != nil {
		return nil, fmt.Errorf("unable to create csr: %v", err)
	}

	var b bytes.Buffer
	bb := bufio.NewWriter(&b)
	if err = pem.Encode(bb, &pem.Block{Type: "CERTIFICATE REQUEST", Bytes: csrBytes}); err != nil {
		return nil, fmt.Errorf("unable to encode pem: %v", err)
	}
	if err = bb.Flush(); err != nil {
		return nil, fmt.Errorf("unable to flush buffer: %v", err)
	}
	return b.Bytes(), nil
}

const (
	nameTypeEmail = 1
	nameTypeDNS   = 2
	nameTypeURI   = 6
	nameTypeIP    = 7
)

// marshalSANs marshals a list of addresses into a the contents of an X.509
// SubjectAlternativeName extension.
func marshalSANs(dnsNames, emailAddresses []string, ipAddresses []net.IP, uris []*url.URL) (derBytes []byte, err error) {
	var rawValues []asn1.RawValue
	for _, name := range dnsNames {
		rawValues = append(rawValues, asn1.RawValue{Tag: nameTypeDNS, Class: 2, Bytes: []byte(name)})
	}
	for _, email := range emailAddresses {
		rawValues = append(rawValues, asn1.RawValue{Tag: nameTypeEmail, Class: 2, Bytes: []byte(email)})
	}
	for _, rawIP := range ipAddresses {
		// If possible, we always want to encode IPv4 addresses in 4 bytes.
		ip := rawIP.To4()
		if ip == nil {
			ip = rawIP
		}
		rawValues = append(rawValues, asn1.RawValue{Tag: nameTypeIP, Class: 2, Bytes: ip})
	}
	for _, uri := range uris {
		rawValues = append(rawValues, asn1.RawValue{Tag: nameTypeURI, Class: 2, Bytes: []byte(uri.String())})
	}
	return asn1.Marshal(rawValues)
}

